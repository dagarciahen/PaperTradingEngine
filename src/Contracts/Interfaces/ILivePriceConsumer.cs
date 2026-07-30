using Contracts.Events; 
namespace Contracts 
{ 
    public interface ILivePriceConsumer

    {
        Task ConsumerAsync(Func<LivePriceUpdatedEvent, Task> onMessage);

    }

}
