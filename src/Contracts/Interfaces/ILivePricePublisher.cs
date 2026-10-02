using Contracts.Events;

namespace Contracts.Interfaces
{
    public interface ILivePricePublisher
    {
        Task PublishAsync(LivePriceUpdatedEvent priceEvent);
        
    }
}
    
