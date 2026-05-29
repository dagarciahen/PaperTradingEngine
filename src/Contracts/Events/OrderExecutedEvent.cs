using System;

namespace Contracts.Events
{

    public class OrderExecutedEvent

    {
        public Guid OrderId {get; set;}
        public bool Completed {get;set;}
        public DateTime CompletionDate {get;set;}
        public string UserIdSeller {get;set;}
        public string UserIdBuyer {get;set;}
        public string Symbol {get; set;}
        public decimal Price {get;set;}
        public decimal Quantity {get;set;}
    }
    
}
