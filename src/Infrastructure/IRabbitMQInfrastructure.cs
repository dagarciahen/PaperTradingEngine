using RabbitMQ.Client;

namespace Infrastructure;

public interface IRabbitMQInfrastructure
{
	IConnection Connection {get;}
	IChannel Channel {get;}

	Task InitializeAsync();
}


