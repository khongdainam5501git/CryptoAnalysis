using CryptoAnalysis.Core.Ingestion;
using Microsoft.AspNetCore.Mvc;

namespace CryptoAnalysis.Api.Controllers;

public record IngestRequest(string[] Assets, int Days = 180);

[ApiController]
[Route("[controller]")]
public class IngestController : ControllerBase
{
    private readonly IngestionService _ingestion;

    public IngestController(IngestionService ingestion) => _ingestion = ingestion;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] IngestRequest req, CancellationToken ct)
    {
        var symbols = (req.Assets is { Length: > 0 }) ? req.Assets : new[] { "BTC", "ETH" };
        var days = req.Days > 0 ? req.Days : 180;
        var results = await _ingestion.IngestAsync(symbols, days, ct);
        return Ok(results);
    }
}
