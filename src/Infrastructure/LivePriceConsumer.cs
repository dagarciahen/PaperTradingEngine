using System.Text;
using System.Text.Json;
using Contracts.Events;
using Contracts.Interfaces;
using RabbitMQ.Client.Events;

namespace Infrastructure;

public class LivePriceConsumer : ILivePriceConsumer
{
    private const string QueueName = "execution_queue";
    private readonly IRabbitMQInfrastructure _rabbitmq;

    public LivePriceConsumer(IRabbitMQInfrastructure rabbitmq)
    {
        _rabbitmq = rabbitmq;
    }

    public async Task ConsumerAsync(Func<LivePriceUpdatedEvent, Task> onMessage)
    {
        var channel = _rabbitmq.Channel;
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async(sender, args) =>
        {
            var json = Encoding.UTF8.GetString(args.Body.Span);
            var priceEvent = JsonSerializer.Deserialize<LivePriceUpdatedEvent>(json);
            if (priceEvent is null)
            {
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
                return;
                
            }
            await onMessage(priceEvent);

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false);

        };
        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal : false,
            exclusive: false,
            arguments : null,
            consumer: consumer);
        
    }
}
