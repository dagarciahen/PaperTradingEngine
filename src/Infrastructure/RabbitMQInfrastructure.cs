using RabbitMQ.Client;
using System;
using Microsoft.Extensions.Options;


namespace Infrastructure
{
	public class RabbitMQInfrasctructure : IRabbitMQInfrastructure
	{
		private IConnection _connection = null!;
		private IChannel _channel = null!; 
		private readonly RabbitMQSettings _rabbitmqSettings;
		public IConnection Connection => _connection!;
		public IChannel Channel => _channel!;
		public RabbitMQInfrasctructure(IOptions<RabbitMQSettings> options)
		{
			_rabbitmqSettings = options.Value;
		}


		public async Task InitializeAsync()
		{
			var factory = new ConnectionFactory {
				HostName = _rabbitmqSettings.HostName, 
				Port = _rabbitmqSettings.Port,
				UserName = _rabbitmqSettings.UserName, Password = _rabbitmqSettings.Password
				};

			 _connection = await factory.CreateConnectionAsync();
			 _channel = await _connection.CreateChannelAsync();

			 var exchange = _rabbitmqSettings.LivePricesExchangeName;
			 var executionQueue = _rabbitmqSettings.ExecutionQueueName;
			 var reportingQueue = _rabbitmqSettings.ReportingQueueName;

			
			await _channel.ExchangeDeclareAsync(exchange: exchange, type: ExchangeType.Fanout, durable: true);
			await _channel.QueueDeclareAsync(queue: executionQueue, durable: true, exclusive: false, autoDelete: false);
			await _channel.QueueDeclareAsync(queue: reportingQueue, durable: true, exclusive: false, autoDelete: false);
			await _channel.QueueBindAsync(queue: executionQueue, exchange: exchange, routingKey: string.Empty);
			await _channel.QueueBindAsync(queue: reportingQueue, exchange: exchange, routingKey: string.Empty);
		}
	}
}

