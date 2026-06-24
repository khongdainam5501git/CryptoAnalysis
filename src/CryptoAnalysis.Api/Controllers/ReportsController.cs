using CryptoAnalysis.Core.Reports;
using Microsoft.AspNetCore.Mvc;

namespace CryptoAnalysis.Api.Controllers;

public record ReportRequest(string[]? Assets);

[ApiController]
[Route("[controller]")]
public class ReportsController : ControllerBase
{
    private readonly PdfReportService _reports;

    public ReportsController(PdfReportService reports) => _reports = reports;

    [HttpPost]
    [Produces("application/pdf")]
    public async Task<IActionResult> Post([FromBody] ReportRequest? req, CancellationToken ct)
    {
        var symbols = (req?.Assets is { Length: > 0 }) ? req.Assets : new[] { "BTC", "ETH" };
        var pdf = await _reports.GenerateAsync(symbols, ct);
        var name = $"crypto_report_{DateTime.UtcNow:yyyyMMdd_HHmm}.pdf";
        return File(pdf, "application/pdf", name);
    }
}
