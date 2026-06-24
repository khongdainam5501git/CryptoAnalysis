namespace CryptoAnalysis.Core.Entities;

public class Candle
{
    public long Id { get; set; }

    public int AssetId { get; set; }
    public Asset? Asset { get; set; }

    public DateTime Ts { get; set; }

    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
