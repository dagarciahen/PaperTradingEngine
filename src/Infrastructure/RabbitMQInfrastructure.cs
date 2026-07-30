using RabbitMQ.Client;

namespace Infrastructure
{
	public class RabbitMQInfrasctructure : IRabbitMQInfrastructure
	{
		private IConnection _connection;
		private IChannel _channel; 
		public IConnection Connection => _connection!;
		public IChannel Channel => _channel!;

		public async Task InitializeAsync()
		{
			var factory = new ConnectionFactory {HostName = "localhost", UserName = "user", Password = "password"};
			 _connection = await factory.CreateConnectionAsync();
			 _channel = await _connection.CreateChannelAsync();
			
			await _channel.ExchangeDeclareAsync(exchange: "live_prices", type: ExchangeType.Fanout);
			await _channel.QueueDeclareAsync(queue: "execution_queue", durable: false, exclusive: false, autoDelete: false);
			await _channel.QueueDeclareAsync(queue: "reporting_queue", durable: false, exclusive: false, autoDelete: false);
			await _channel.QueueBindAsync(queue: "execution_queue", exchange: "live_prices", routingKey: string.Empty);
			await _channel.QueueBindAsync(queue:"reporting_queue", exchange: "live_prices", routingKey: string.Empty);
		}
	}
}

