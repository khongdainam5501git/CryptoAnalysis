using CryptoAnalysis.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptoAnalysis.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CandlesController : ControllerBase
{
    private readonly CryptoDbContext _db;

    public CandlesController(CryptoDbContext db) => _db = db;

    [HttpGet("/assets")]
    public async Task<IActionResult> Assets(CancellationToken ct)
    {
        var assets = await _db.Assets
            .Select(a => new
            {
                a.Id,
                a.Symbol,
                a.Name,
                a.CoinGeckoId,
                Candles = _db.Candles.Count(c => c.AssetId == a.Id)
            })
            .ToListAsync(ct);
        return Ok(assets);
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? symbol,
        [FromQuery] int limit = 50,
        [FromQuery] string order = "desc",
        CancellationToken ct = default)
    {
        var q = _db.Candles.Include(c => c.Asset).AsQueryable();

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            var s = symbol.Trim().ToUpperInvariant();
            q = q.Where(c => c.Asset!.Symbol == s);
        }

        q = order.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? q.OrderBy(c => c.Ts)
            : q.OrderByDescending(c => c.Ts);

        var rows = await q
            .Take(Math.Clamp(limit, 1, 1000))
            .Select(c => new
            {
                Symbol = c.Asset!.Symbol,
                c.Ts,
                c.Open,
                c.High,
                c.Low,
                c.Close,
                c.Volume
            })
            .ToListAsync(ct);

        return Ok(new { count = rows.Count, items = rows });
    }
}
