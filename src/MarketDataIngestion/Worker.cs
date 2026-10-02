using System.Globalization;
using System.Net.WebSockets;
using Contracts.Events;
using Contracts.Interfaces;
using Infrastructure;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MarketDataIngestion
{
    public class Worker: BackgroundService
    {
        private readonly ILogger<Worker> _logger; 
        private readonly ILivePricePublisher _publisher;
		private readonly IRabbitMQInfrastructure _rabbitmq;
        private readonly BinanceSettings _binance;

    public Worker(ILogger<Worker> logger, ILivePricePublisher publisher,
    IRabbitMQInfrastructure rabbitmq, IOptions<BinanceSettings> binance) {
            _logger = logger;
            _publisher = publisher;
			_rabbitmq = rabbitmq;
            _binance = binance.Value;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {

            var streamUrl = new Uri(_binance.BuildStreamUrl());
            await _rabbitmq.InitializeAsync();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConsumeBinanceAsync(streamUrl, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Conexion with Binance lost, Reconnected 5s");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

    private async Task ConsumeBinanceAsync(Uri streamUrl, CancellationToken stoppingToken)
    {
        using var socket = new ClientWebSocket();
        _logger.LogInformation("Connected to Binance to {Url}", streamUrl);
        await socket.ConnectAsync(streamUrl, stoppingToken);
        _logger.LogInformation("Connected.. listening trades");
        //1 trade is 200byte, make 8kb
        var buffer = new byte[8192];

        while (socket.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
            {
                
                var payload = await ReceiveTextAsync(socket, buffer, stoppingToken);
                if (payload is null)
                {
                    break;

                }
                var priceEvent = ParseTrade(payload);
                if(priceEvent is null)
                {
                    continue;
                }
                await _publisher.PublishAsync(priceEvent);

                _logger.LogInformation(
                    "{Symbol} {Price} qty {Quantity} @ {Timestamp:HH:mm:ss.fff}",
                    priceEvent.Symbol, priceEvent.Price, priceEvent.Quantity, priceEvent.Timestamp);
            }
    }
    private static async Task<string?> ReceiveTextAsync(
        ClientWebSocket socket, byte[] buffer, CancellationToken stoppingToken)
        {
            using var ms = new MemoryStream();
            var endOfMessage = false;
            while (!endOfMessage)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, stoppingToken);
                    return null;
                }
                ms.Write(buffer, 0, result.Count);
                endOfMessage = result.EndOfMessage;
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        
        private static LivePriceUpdatedEvent? ParseTrade(string json)
        {
            BinanceEnvelope? envelope;


            try
            {
                envelope = JsonSerializer.Deserialize<BinanceEnvelope>(json);
            }
            catch (JsonException)
            {
                return null;
            }
            if (envelope is null)
            {
                return null;
            }
            if (envelope.Data is null)
            {
                return null;
            }
            BinanceTrade trade = envelope.Data;
            if (string.IsNullOrEmpty(trade.Symbol))
            {
                return null;
            }
            var priceEvent = new LivePriceUpdatedEvent
            {
                Symbol = trade.Symbol,
                Price = decimal.Parse(trade.Price, CultureInfo.InvariantCulture),
                Quantity = decimal.Parse(trade.Quantity, CultureInfo.InvariantCulture),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(trade.TradeTime).UtcDateTime
            };

    return priceEvent;
}
            }

        }
          