using System;

namespace Contracts.Events
{
    public class LivePriceUpdatedEvent
    {
        public string Symbol {get;set;} = string.Empty;
        public decimal Price {get;set;}
        public DateTime Timestamp {get;set;}
        public decimal Quantity {get; set;}
        
    }
}