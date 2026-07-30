using Contracts.Events;
using Infrastructure;
using System.Text;
using System.Text.Json;
using System;

namespace MarketDataIngestion
{
    public class Worker: BackgroundService
    {
        private readonly ILogger<Worker> _logger; 
        private readonly ILivePricePublisher _publisher;
		private readonly IRabbitMQInfrastructure _rabbitmq;

public Worker(ILogger<Worker> logger, ILivePricePublisher publisher, IRabbitMQInfrastructure rabbitmq) {
            _logger = logger;
            _publisher = publisher;
			_rabbitmq = rabbitmq;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            string path = "../Contracts/utils/prices.json";
            List<LivePriceUpdatedEvent> priceEvents = new List<LivePriceUpdatedEvent>();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                priceEvents = JsonSerializer.Deserialize<List<LivePriceUpdatedEvent>>(json) ?? new List<LivePriceUpdatedEvent>();
            }

			await _rabbitmq.InitializeAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);

				for (int i = 0; i < priceEvents.Count; i++)
				{
					var priceEventEx = priceEvents[i];
					await _publisher.PublishAsync(priceEventEx);
					_logger.LogInformation("Event {Symbol} published at {time} at price {price}", priceEventEx.Symbol, priceEventEx.Timestamp, priceEventEx.Price);
					await Task.Delay(3000);

				}

            }
        }
    }
}
