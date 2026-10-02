using Contracts.Events; 

namespace Contracts.Interfaces
{ 
    public interface ILivePriceConsumer

    {
        Task ConsumerAsync(Func<LivePriceUpdatedEvent, Task> onMessage);

    }

}
