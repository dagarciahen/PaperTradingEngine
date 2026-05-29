using System;

namespace Contracts.Events
{

    public class OrderCreatedEvent
    {
        public Guid OrderId {get;set;}
        public string UserId {get;set;}
        public string Symbol {get;set;}
        public string Side {get;set;} //ex buy, sell
        public decimal Quantity {get;set;}
        public decimal TargetPrice {get;set;}
        public DateTime CreatedAt {get;set;}
    }
}

