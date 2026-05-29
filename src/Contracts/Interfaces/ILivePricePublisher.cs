namespace Contracts.Events
{
    public interface ILivePricePublisher
    {
        Task InitializeAsync();
        Task PublishAsync(LivePriceUpdatedEvent priceEvent);
        
    }
}
    