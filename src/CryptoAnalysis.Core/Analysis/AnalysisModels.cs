namespace CryptoAnalysis.Core.Analysis;

public record AssetStats(
    string Symbol,
    string Name,
    int CandleCount,
    DateTime From,
    DateTime To,
    double MinClose,
    double MaxClose,
    double MeanClose,
    double MedianClose,
    double StdClose,
    double MeanReturn,
    double Volatility,            // std of log-returns
    double AnnualizedVolatility   // Volatility * sqrt(candles per year)
);

public record CorrelationResult(
    string SymbolA,
    string SymbolB,
    int N,
    double Pearson
);

public record AnalysisResult(
    IReadOnlyList<AssetStats> Assets,
    CorrelationResult? Correlation,
    int CandleIntervalDays,
    DateTime GeneratedAt
);
