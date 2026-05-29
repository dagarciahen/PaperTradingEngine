using System;

namespace Contracts.Events
{
    public class LivePriceUpdatedEvent
    {
        public string Symbol {get;set;} // btc usdt
        public decimal Price {get;set;}
        public DateTime Timestamp {get;set;}
    }
}