namespace CryptoAnalysis.Core.Ingestion;

public class CoinGeckoOptions
{
    public const string Section = "CoinGecko";

    public string BaseUrl { get; set; } = "https://api.coingecko.com/api/v3/";

    // sent as the x-cg-demo-api-key header
    public string ApiKey { get; set; } = string.Empty;

    public string VsCurrency { get; set; } = "usd";
}
