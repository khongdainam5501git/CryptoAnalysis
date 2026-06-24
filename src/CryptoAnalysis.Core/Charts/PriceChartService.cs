using ScottPlot;

namespace CryptoAnalysis.Core.Charts;

public class PriceChartService
{
    private static readonly Color[] Palette =
    {
        Colors.Blue, Colors.Red, Colors.Green, Colors.Purple
    };

    // rebases every series to 100 at the start so prices stay comparable
    public byte[] BuildNormalizedComparison(
        IReadOnlyList<(string Symbol, List<(DateTime Ts, double Close)> Series)> data,
        int width = 900, int height = 450)
    {
        var plot = new Plot();
        int idx = 0;
        foreach (var (symbol, series) in data)
        {
            if (series.Count == 0) continue;
            double baseClose = series[0].Close;

            double[] xs = series.Select(p => p.Ts.ToOADate()).ToArray();
            double[] ys = series.Select(p => baseClose == 0 ? 100.0 : p.Close / baseClose * 100.0).ToArray();

            var line = plot.Add.Scatter(xs, ys);
            line.LegendText = symbol;
            line.MarkerSize = 0;
            line.LineWidth = 2;
            line.Color = Palette[idx % Palette.Length];
            idx++;
        }

        plot.Axes.DateTimeTicksBottom();
        plot.Title("Price dynamics (normalized to 100)");
        plot.XLabel("Date");
        plot.YLabel("Price index, start = 100");
        plot.ShowLegend();

        return plot.GetImage(width, height).GetImageBytes();
    }

    public byte[] BuildPriceChart(
        string symbol, List<(DateTime Ts, double Close)> series,
        int width = 900, int height = 350)
    {
        var plot = new Plot();
        if (series.Count > 0)
        {
            double[] xs = series.Select(p => p.Ts.ToOADate()).ToArray();
            double[] ys = series.Select(p => p.Close).ToArray();

            var line = plot.Add.Scatter(xs, ys);
            line.LegendText = symbol;
            line.MarkerSize = 0;
            line.LineWidth = 2;
            line.Color = Palette[0];
        }

        plot.Axes.DateTimeTicksBottom();
        plot.Title($"{symbol} closing price (USD)");
        plot.XLabel("Date");
        plot.YLabel("USD");

        return plot.GetImage(width, height).GetImageBytes();
    }
}
