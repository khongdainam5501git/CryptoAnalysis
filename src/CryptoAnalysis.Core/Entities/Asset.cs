namespace CryptoAnalysis.Core.Entities;

public class Asset
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CoinGeckoId { get; set; } = string.Empty;
    public decimal? MarketCap { get; set; }

    public List<Candle> Candles { get; set; } = new();
    public List<Metric> Metrics { get; set; } = new();
}
