using System.Globalization;
using CryptoAnalysis.Core.Analysis;
using CryptoAnalysis.Core.Charts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CryptoAnalysis.Core.Reports;

public class PdfReportService
{
    private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

    private readonly PriceChartService _charts;
    private readonly AnalysisService _analysis;

    public PdfReportService(PriceChartService charts, AnalysisService analysis)
    {
        _charts = charts;
        _analysis = analysis;
    }

    public async Task<byte[]> GenerateAsync(IReadOnlyList<string> symbols, CancellationToken ct = default)
    {
        var result = await _analysis.AnalyzeAsync(symbols, persist: true, ct);

        var series = new List<(string Symbol, List<(DateTime Ts, double Close)> Series)>();
        foreach (var st in result.Assets)
            series.Add((st.Symbol, await _analysis.LoadCloseSeriesAsync(st.Symbol, ct)));

        byte[]? comparison = series.Count >= 1 ? _charts.BuildNormalizedComparison(series) : null;
        var priceCharts = series
            .Where(x => x.Series.Count > 0)
            .Select(x => (x.Symbol, Png: _charts.BuildPriceChart(x.Symbol, x.Series)))
            .ToList();

        return Build(result, comparison, priceCharts);
    }

    private byte[] Build(
        AnalysisResult result,
        byte[]? comparisonChart,
        IReadOnlyList<(string Symbol, byte[] Png)> priceCharts)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().Text("Cryptocurrency Market Analysis Report")
                        .FontSize(18).Bold().FontColor(Colors.Grey.Darken3);
                    col.Item().Text("Analysis system based on open market data (CoinGecko)")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(2).Text(
                        $"Generated: {result.GeneratedAt:yyyy-MM-dd HH:mm} UTC, candle interval ~{result.CandleIntervalDays} days")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Text("1. Statistical indicators").FontSize(13).Bold();
                    col.Item().Element(c => MetricsTable(c, result));

                    col.Item().Text("2. Correlation (Pearson)").FontSize(13).Bold();
                    col.Item().Element(c => CorrelationBlock(c, result.Correlation));

                    col.Item().Text("3. Charts").FontSize(13).Bold();
                    if (comparisonChart is not null)
                        col.Item().Image(comparisonChart).FitWidth();
                    foreach (var (_, png) in priceCharts)
                        col.Item().Image(png).FitWidth();
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("CryptoAnalysis - page ");
                    txt.CurrentPageNumber();
                    txt.Span(" of ");
                    txt.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void MetricsTable(IContainer container, AnalysisResult result)
    {
        var assets = result.Assets;
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2.4f);
                foreach (var _ in assets) cols.RelativeColumn(1.6f);
            });

            HeaderCell(table, "Indicator");
            foreach (var a in assets) HeaderCell(table, $"{a.Symbol} ({a.Name})");

            Row(table, "Period", assets, a => $"{a.From:yyyy-MM-dd} - {a.To:yyyy-MM-dd}");
            Row(table, "Candle count", assets, a => a.CandleCount.ToString(Ci));
            Row(table, "Min price (USD)", assets, a => Money(a.MinClose));
            Row(table, "Max price (USD)", assets, a => Money(a.MaxClose));
            Row(table, "Mean price (USD)", assets, a => Money(a.MeanClose));
            Row(table, "Median (USD)", assets, a => Money(a.MedianClose));
            Row(table, "Price std. deviation", assets, a => Money(a.StdClose));
            Row(table, "Mean return / candle", assets, a => Pct(a.MeanReturn));
            Row(table, "Volatility (σ of log-returns)", assets, a => Pct(a.Volatility));
            Row(table, "Annualized volatility", assets, a => Pct(a.AnnualizedVolatility));
        });
    }

    private static void CorrelationBlock(IContainer container, CorrelationResult? corr)
    {
        if (corr is null)
        {
            container.Text("Not enough data to compute correlation.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var strength = Math.Abs(corr.Pearson) switch
        {
            >= 0.8 => "very strong",
            >= 0.6 => "strong",
            >= 0.4 => "moderate",
            >= 0.2 => "weak",
            _ => "very weak"
        };
        var sign = corr.Pearson >= 0 ? "positive" : "negative";

        container.Column(col =>
        {
            col.Item().Text(txt =>
            {
                txt.Span($"Pearson coefficient {corr.SymbolA}–{corr.SymbolB} (on log-returns, N = {corr.N}): ");
                txt.Span(corr.Pearson.ToString("F4", Ci)).Bold();
            });
            col.Item().Text($"Interpretation: {sign}, {strength} linear relationship between the assets' returns.")
                .FontColor(Colors.Grey.Darken1);
        });
    }


    private static void HeaderCell(TableDescriptor table, string text)
    {
        table.Cell().Element(CellHeader).Text(text).Bold().FontColor(Colors.White);
    }

    private static void Row(TableDescriptor table, string label,
        IReadOnlyList<AssetStats> assets, Func<AssetStats, string> value)
    {
        table.Cell().Element(CellLabel).Text(label).SemiBold();
        foreach (var a in assets)
            table.Cell().Element(CellBody).Text(value(a));
    }

    private static IContainer CellHeader(IContainer c) =>
        c.Background(Colors.Grey.Darken2).Border(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(5).PaddingHorizontal(6);

    private static IContainer CellLabel(IContainer c) =>
        c.Background(Colors.Grey.Lighten4).Border(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(4).PaddingHorizontal(6);

    private static IContainer CellBody(IContainer c) =>
        c.Border(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(4).PaddingHorizontal(6);

    private static string Money(double v) => v.ToString("N2", Ci);
    private static string Pct(double v) => (v * 100).ToString("F2", Ci) + " %";
}
