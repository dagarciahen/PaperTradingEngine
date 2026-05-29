using Contracts.Events; 
namespace Contracts 
{ 
    public interface ILivePriceConsumer

    {
        Task InitializeAsync();
        Task ConsumerAsync(Func<LivePriceUpdatedEvent, Task> onMessage);

    }

}
