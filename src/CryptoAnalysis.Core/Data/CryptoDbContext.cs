using CryptoAnalysis.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoAnalysis.Core.Data;

public class CryptoDbContext : DbContext
{
    public CryptoDbContext(DbContextOptions<CryptoDbContext> options) : base(options) { }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Candle> Candles => Set<Candle>();
    public DbSet<Metric> Metrics => Set<Metric>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Asset>(e =>
        {
            e.ToTable("asset");
            e.HasKey(x => x.Id);
            e.Property(x => x.Symbol).HasMaxLength(16).IsRequired();
            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.Property(x => x.CoinGeckoId).HasMaxLength(64).IsRequired();
            e.Property(x => x.MarketCap).HasColumnType("numeric(28,2)");
            e.HasIndex(x => x.Symbol).IsUnique();
        });

        b.Entity<Candle>(e =>
        {
            e.ToTable("candle");
            e.HasKey(x => x.Id);
            e.Property(x => x.Open).HasColumnType("numeric(28,8)");
            e.Property(x => x.High).HasColumnType("numeric(28,8)");
            e.Property(x => x.Low).HasColumnType("numeric(28,8)");
            e.Property(x => x.Close).HasColumnType("numeric(28,8)");
            e.Property(x => x.Volume).HasColumnType("numeric(28,8)");
            e.HasIndex(x => new { x.AssetId, x.Ts }).IsUnique();
            e.HasOne(x => x.Asset)
             .WithMany(a => a.Candles)
             .HasForeignKey(x => x.AssetId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Metric>(e =>
        {
            e.ToTable("metric");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(32).IsRequired();
            e.Property(x => x.Period).HasMaxLength(16).IsRequired();
            e.HasIndex(x => new { x.AssetId, x.Type, x.Period });
            e.HasOne(x => x.Asset)
             .WithMany(a => a.Metrics)
             .HasForeignKey(x => x.AssetId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
