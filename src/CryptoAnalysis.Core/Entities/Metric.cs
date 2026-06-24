namespace CryptoAnalysis.Core.Entities;

public class Metric
{
    public long Id { get; set; }

    public int? AssetId { get; set; }   // null for pair-level metrics
    public Asset? Asset { get; set; }

    public string Type { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;

    public double Value { get; set; }

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
