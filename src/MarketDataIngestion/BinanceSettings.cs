
namespace MarketDataIngestion;

public class BinanceSettings
{
    public const string SectionName = "Binance";
    public string BaseUrl {get; set;} = string.Empty;
    public string[] Streams {get; set;} = [];

    public string BuildStreamUrl() => 
    $"{BaseUrl.TrimEnd('/')}/stream?streams={string.Join('/', Streams)}";

}