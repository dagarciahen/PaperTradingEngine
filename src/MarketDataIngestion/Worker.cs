using Contracts.Events;
using System;

namespace MarketDataIngestion
{
    public class Worker: BackgroundService
    {
        private readonly ILogger<Worker> _logger; 
        private readonly ILivePricePublisher _publisher;

        public Worker(ILogger<Worker> logger, ILivePricePublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
			Random rdn = new Random();
			List<LivePriceUpdatedEvent> priceEvents = new List<LivePriceUpdatedEvent>();
			for (int i = 0; i <10; i++)
			{
				var newPrice = Math.Round((decimal)rdn.NextDouble() *100m,2); //nextdouble random num (0.0 and 1.0) convert to decimal,	convert 0.3745 to 37.45 with *100m round to 2 
				DateTime Now = DateTime.Now;
				
				priceEvents.Add(new LivePriceUpdatedEvent
				{
					
					Symbol = "BTCUSDT",
					Price = newPrice,
					Timestamp = Now
				});
			}


            await _publisher.InitializeAsync();
 
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
