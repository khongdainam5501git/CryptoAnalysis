using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CryptoAnalysis.Core.Ingestion;

public record RawCandle(DateTime Ts, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);

public class CoinGeckoClient
{
    private readonly HttpClient _http;
    private readonly CoinGeckoOptions _opt;
    private readonly ILogger<CoinGeckoClient> _log;

    public CoinGeckoClient(HttpClient http, IOptions<CoinGeckoOptions> opt, ILogger<CoinGeckoClient> log)
    {
        _http = http;
        _opt = opt.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<RawCandle>> GetCandlesAsync(string coinGeckoId, int days, CancellationToken ct = default)
    {
        var ohlc = await GetOhlcAsync(coinGeckoId, days, ct);
        var volumes = await GetDailyVolumesAsync(coinGeckoId, days, ct);

        // attach volume to each candle by day
        var candles = new List<RawCandle>(ohlc.Count);
        foreach (var rc in ohlc)
        {
            var day = DateOnly.FromDateTime(rc.Ts);
            volumes.TryGetValue(day, out var vol);
            candles.Add(rc with { Volume = vol });
        }

        _log.LogInformation("CoinGecko {Coin}: {Count} OHLC candles, {Vols} volume points.",
            coinGeckoId, candles.Count, volumes.Count);
        return candles;
    }

    // GET /coins/{id}/ohlc -> [[ts, open, high, low, close], ...]
    private async Task<List<RawCandle>> GetOhlcAsync(string coinGeckoId, int days, CancellationToken ct)
    {
        var url = $"coins/{coinGeckoId}/ohlc?vs_currency={_opt.VsCurrency}&days={days}";
        var rows = await _http.GetFromJsonAsync<List<JsonElement>>(url, ct)
                   ?? throw new InvalidOperationException($"Empty OHLC for {coinGeckoId}");

        var result = new List<RawCandle>(rows.Count);
        foreach (var row in rows)
        {
            var ts = DateTimeOffset.FromUnixTimeMilliseconds(row[0].GetInt64()).UtcDateTime;
            result.Add(new RawCandle(
                ts,
                ToDecimal(row[1]),
                ToDecimal(row[2]),
                ToDecimal(row[3]),
                ToDecimal(row[4]),
                0m));
        }
        return result;
    }

    // GET /coins/{id}/market_chart -> total_volumes grouped by day
    private async Task<Dictionary<DateOnly, decimal>> GetDailyVolumesAsync(string coinGeckoId, int days, CancellationToken ct)
    {
        var url = $"coins/{coinGeckoId}/market_chart?vs_currency={_opt.VsCurrency}&days={days}&interval=daily";
        var root = await _http.GetFromJsonAsync<JsonElement>(url, ct);

        var map = new Dictionary<DateOnly, decimal>();
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("total_volumes", out var vols))
        {
            foreach (var pair in vols.EnumerateArray())
            {
                var ts = DateTimeOffset.FromUnixTimeMilliseconds(pair[0].GetInt64()).UtcDateTime;
                map[DateOnly.FromDateTime(ts)] = ToDecimal(pair[1]);
            }
        }
        return map;
    }

    private static decimal ToDecimal(JsonElement e) =>
        e.ValueKind == JsonValueKind.Number
            ? e.GetDecimal()
            : decimal.Parse(e.GetString() ?? "0", CultureInfo.InvariantCulture);
}
