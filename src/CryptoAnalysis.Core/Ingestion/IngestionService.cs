using CryptoAnalysis.Core.Data;
using CryptoAnalysis.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoAnalysis.Core.Ingestion;

public record IngestResult(string Symbol, int Fetched, int Inserted, int Skipped, int TotalInDb);

public class IngestionService
{
    private static readonly Dictionary<string, (string CoinGeckoId, string Name)> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC"] = ("bitcoin", "Bitcoin"),
        ["ETH"] = ("ethereum", "Ethereum"),
    };

    private readonly CryptoDbContext _db;
    private readonly CoinGeckoClient _client;
    private readonly ILogger<IngestionService> _log;

    public IngestionService(CryptoDbContext db, CoinGeckoClient client, ILogger<IngestionService> log)
    {
        _db = db;
        _client = client;
        _log = log;
    }

    public async Task<IReadOnlyList<IngestResult>> IngestAsync(IEnumerable<string> symbols, int days, CancellationToken ct = default)
    {
        var results = new List<IngestResult>();
        foreach (var raw in symbols)
        {
            var symbol = raw.Trim().ToUpperInvariant();
            if (!Known.TryGetValue(symbol, out var meta))
            {
                _log.LogWarning("Skipping unsupported symbol: {Symbol}", symbol);
                continue;
            }

            var asset = await EnsureAssetAsync(symbol, meta.CoinGeckoId, meta.Name, ct);
            var fetched = await _client.GetCandlesAsync(meta.CoinGeckoId, days, ct);

            // skip candles already in the DB
            var existing = await _db.Candles
                .Where(c => c.AssetId == asset.Id)
                .Select(c => c.Ts)
                .ToListAsync(ct);
            var existingSet = existing.ToHashSet();

            var toInsert = new List<Candle>();
            var seen = new HashSet<DateTime>();
            foreach (var rc in fetched)
            {
                if (existingSet.Contains(rc.Ts) || !seen.Add(rc.Ts))
                    continue;
                toInsert.Add(new Candle
                {
                    AssetId = asset.Id,
                    Ts = rc.Ts,
                    Open = rc.Open,
                    High = rc.High,
                    Low = rc.Low,
                    Close = rc.Close,
                    Volume = rc.Volume,
                });
            }

            if (toInsert.Count > 0)
            {
                _db.Candles.AddRange(toInsert);
                await _db.SaveChangesAsync(ct);
            }

            var total = await _db.Candles.CountAsync(c => c.AssetId == asset.Id, ct);
            results.Add(new IngestResult(symbol, fetched.Count, toInsert.Count, fetched.Count - toInsert.Count, total));
            _log.LogInformation("{Symbol}: fetched={F} inserted={I} total={T}", symbol, fetched.Count, toInsert.Count, total);
        }
        return results;
    }

    private async Task<Asset> EnsureAssetAsync(string symbol, string coinGeckoId, string name, CancellationToken ct)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Symbol == symbol, ct);
        if (asset is null)
        {
            asset = new Asset { Symbol = symbol, Name = name, CoinGeckoId = coinGeckoId };
            _db.Assets.Add(asset);
            await _db.SaveChangesAsync(ct);
        }
        return asset;
    }
}
