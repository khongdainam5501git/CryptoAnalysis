namespace CryptoAnalysis.Core.Analysis;

public static class Returns
{
    // r_t = (close_t - close_{t-1}) / close_{t-1}
    public static double[] Simple(IReadOnlyList<double> closes)
    {
        if (closes.Count < 2) return Array.Empty<double>();
        var r = new double[closes.Count - 1];
        for (int i = 1; i < closes.Count; i++)
        {
            var prev = closes[i - 1];
            r[i - 1] = prev == 0 ? 0 : (closes[i] - prev) / prev;
        }
        return r;
    }

    // r_t = ln(close_t / close_{t-1})
    public static double[] Log(IReadOnlyList<double> closes)
    {
        if (closes.Count < 2) return Array.Empty<double>();
        var r = new double[closes.Count - 1];
        for (int i = 1; i < closes.Count; i++)
        {
            var prev = closes[i - 1];
            r[i - 1] = (prev <= 0 || closes[i] <= 0) ? 0 : Math.Log(closes[i] / prev);
        }
        return r;
    }
}
