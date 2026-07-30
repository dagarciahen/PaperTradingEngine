using System.Text;
using System.Text.Json;
using Contracts.Events; 
using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Infrastructure;
    
namespace MarketDataIngestion
{
    //los constructores en c# no pueden ser async ni usar await. y readonly solo permite asignar en declaracion o constructor pero no en otros metodos 

    public class LivePricePublisher : ILivePricePublisher
    {
		private readonly IRabbitMQInfrastructure _rabbitmq;

		public LivePricePublisher(IRabbitMQInfrastructure rabbitmq)
		{
			_rabbitmq = rabbitmq;

        }
		
        public async Task PublishAsync(LivePriceUpdatedEvent priceEvent)
        {
			var channel = _rabbitmq.Channel;

            var message = JsonSerializer.Serialize(priceEvent);
            var body = Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "live_prices", routingKey: string.Empty, body: body);
            
        }
    }
}


