using CryptoAnalysis.Core.Analysis;
using MathNet.Numerics.Statistics;
using Xunit;

namespace CryptoAnalysis.Tests;

public class ReturnsTests
{
    [Fact]
    public void Simple_TwoPoints_ComputesPercentChange()
    {
        var closes = new double[] { 100, 110 };
        var r = Returns.Simple(closes);
        Assert.Single(r);
        Assert.Equal(0.10, r[0], 10);
    }

    [Fact]
    public void Simple_LengthIsNMinusOne()
    {
        var closes = new double[] { 10, 20, 30, 40 };
        Assert.Equal(closes.Length - 1, Returns.Simple(closes).Length);
    }

    [Fact]
    public void Log_MatchesManualLogRatio()
    {
        var closes = new double[] { 100, 200 };
        var r = Returns.Log(closes);
        Assert.Single(r);
        Assert.Equal(System.Math.Log(2.0), r[0], 10);
    }

    [Fact]
    public void Returns_EmptyOrSingle_ReturnsEmpty()
    {
        Assert.Empty(Returns.Simple(new double[] { 42 }));
        Assert.Empty(Returns.Log(System.Array.Empty<double>()));
    }

    [Fact]
    public void Log_GuardsAgainstNonPositivePrices()
    {
        var r = Returns.Log(new double[] { 0, 100 });
        Assert.Equal(0.0, r[0], 10);
    }
}

public class CorrelationTests
{
    [Fact]
    public void Pearson_PerfectlyCorrelated_IsOne()
    {
        var x = new double[] { 1, 2, 3, 4, 5 };
        var y = new double[] { 2, 4, 6, 8, 10 };
        Assert.Equal(1.0, Correlation.Pearson(x, y), 6);
    }

    [Fact]
    public void Pearson_PerfectlyInverse_IsMinusOne()
    {
        var x = new double[] { 1, 2, 3, 4, 5 };
        var y = new double[] { 10, 8, 6, 4, 2 };
        Assert.Equal(-1.0, Correlation.Pearson(x, y), 6);
    }
}
