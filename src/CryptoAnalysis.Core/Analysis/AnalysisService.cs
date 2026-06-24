using CryptoAnalysis.Core.Data;
using CryptoAnalysis.Core.Entities;
using MathNet.Numerics.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoAnalysis.Core.Analysis;

public class AnalysisService
{
    private readonly CryptoDbContext _db;
    private readonly ILogger<AnalysisService> _log;

    public AnalysisService(CryptoDbContext db, ILogger<AnalysisService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<(DateTime Ts, double Close)>> LoadCloseSeriesAsync(string symbol, CancellationToken ct = default)
    {
        var s = symbol.Trim().ToUpperInvariant();
        var rows = await _db.Candles
            .Where(c => c.Asset!.Symbol == s)
            .OrderBy(c => c.Ts)
            .Select(c => new { c.Ts, c.Close })
            .ToListAsync(ct);
        return rows.Select(r => (r.Ts, (double)r.Close)).ToList();
    }

    public async Task<AnalysisResult> AnalyzeAsync(IReadOnlyList<string> symbols, bool persist = true, CancellationToken ct = default)
    {
        var series = new Dictionary<string, List<(DateTime Ts, double Close)>>(StringComparer.OrdinalIgnoreCase);
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in symbols)
        {
            var s = raw.Trim().ToUpperInvariant();
            if (series.ContainsKey(s)) continue;
            series[s] = await LoadCloseSeriesAsync(s, ct);
            names[s] = (await _db.Assets.Where(a => a.Symbol == s).Select(a => a.Name).FirstOrDefaultAsync(ct)) ?? s;
        }

        var intervalDays = EstimateIntervalDays(series.Values.FirstOrDefault(v => v.Count >= 2));
        var candlesPerYear = intervalDays > 0 ? 365.0 / intervalDays : 1.0;

        var statsList = new List<AssetStats>();
        foreach (var s in series.Keys)
        {
            var data = series[s];
            if (data.Count == 0)
            {
                _log.LogWarning("No candles for {Symbol}, skipping.", s);
                continue;
            }

            var closes = data.Select(d => d.Close).ToArray();
            var logReturns = Returns.Log(closes);

            var volatility = logReturns.Length > 1 ? logReturns.StandardDeviation() : 0.0;
            var annualized = volatility * Math.Sqrt(candlesPerYear);

            statsList.Add(new AssetStats(
                Symbol: s,
                Name: names[s],
                CandleCount: data.Count,
                From: data.First().Ts,
                To: data.Last().Ts,
                MinClose: closes.Min(),
                MaxClose: closes.Max(),
                MeanClose: closes.Mean(),
                MedianClose: closes.Median(),
                StdClose: closes.Length > 1 ? closes.StandardDeviation() : 0.0,
                MeanReturn: logReturns.Length > 0 ? logReturns.Mean() : 0.0,
                Volatility: volatility,
                AnnualizedVolatility: annualized
            ));
        }

        CorrelationResult? correlation = null;
        var symList = statsList.Select(x => x.Symbol).ToList();
        if (symList.Count >= 2)
        {
            correlation = ComputePearson(symList[0], symList[1], series);
        }

        if (persist)
            await PersistAsync(statsList, correlation, intervalDays, ct);

        return new AnalysisResult(statsList, correlation, intervalDays, DateTime.UtcNow);
    }

    private static CorrelationResult? ComputePearson(
        string a, string b, Dictionary<string, List<(DateTime Ts, double Close)>> series)
    {
        var mapA = series[a].ToDictionary(x => x.Ts, x => x.Close);
        var mapB = series[b].ToDictionary(x => x.Ts, x => x.Close);

        // align on timestamps present in both series
        var commonTs = mapA.Keys.Intersect(mapB.Keys).OrderBy(t => t).ToList();
        if (commonTs.Count < 3) return null;

        var closesA = commonTs.Select(t => mapA[t]).ToArray();
        var closesB = commonTs.Select(t => mapB[t]).ToArray();

        var ra = Returns.Log(closesA);
        var rb = Returns.Log(closesB);
        if (ra.Length < 2) return null;

        var pearson = Correlation.Pearson(ra, rb);
        return new CorrelationResult(a, b, ra.Length, pearson);
    }

    private static int EstimateIntervalDays(List<(DateTime Ts, double Close)>? data)
    {
        if (data is null || data.Count < 2) return 4;
        var gaps = new List<double>();
        for (int i = 1; i < data.Count; i++)
            gaps.Add((data[i].Ts - data[i - 1].Ts).TotalDays);
        var median = gaps.Median();
        var rounded = (int)Math.Round(median);
        return rounded < 1 ? 1 : rounded;
    }

    private async Task PersistAsync(
        IReadOnlyList<AssetStats> stats, CorrelationResult? corr, int intervalDays, CancellationToken ct)
    {
        var old = await _db.Metrics.ToListAsync(ct);
        if (old.Count > 0) _db.Metrics.RemoveRange(old);

        var now = DateTime.UtcNow;
        foreach (var st in stats)
        {
            var assetId = await _db.Assets.Where(a => a.Symbol == st.Symbol).Select(a => (int?)a.Id).FirstOrDefaultAsync(ct);
            _db.Metrics.Add(new Metric
            {
                AssetId = assetId,
                Type = "volatility",
                Period = "overall",
                Value = st.Volatility,
                ComputedAt = now
            });
        }

        if (corr is not null)
        {
            _db.Metrics.Add(new Metric
            {
                AssetId = null, // pair-level
                Type = "correlation",
                Period = "overall",
                Value = corr.Pearson,
                ComputedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Saved {Count} metrics (interval ~{Days} days).", stats.Count + (corr is null ? 0 : 1), intervalDays);
    }
}
