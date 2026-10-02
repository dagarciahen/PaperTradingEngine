using System.Text.Json.Serialization;

namespace MarketDataIngestion;

// { "stream": "btcusdt@trade", "data": { ... } }
internal class BinanceEnvelope
{
    [JsonPropertyName("data")]
    public BinanceTrade? Data { get; set; }
}
// { "s": "BTCUSDT", "p": "95000.12", "q": "0.5", "T": 1699999999999 }
internal sealed record BinanceTrade(
    [property: JsonPropertyName("s")] string Symbol,
    [property: JsonPropertyName("p")] string Price,
    [property: JsonPropertyName("q")] string Quantity,
    [property: JsonPropertyName("T")] long TradeTime);