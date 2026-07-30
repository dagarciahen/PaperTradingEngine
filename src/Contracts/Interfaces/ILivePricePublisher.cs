namespace Contracts.Events
{
    public interface ILivePricePublisher
    {
        Task PublishAsync(LivePriceUpdatedEvent priceEvent);
        
    }
}
    
