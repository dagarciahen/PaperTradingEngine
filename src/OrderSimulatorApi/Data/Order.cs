using Contracts.Domain.V1;


namespace OrderSimulatorApi;

public class Order
{
    public int OrderId {get;set;}
    public int UserId {get;set;}
    public string Symbol {get;set;}
    public OrderType Type {get;set;}
    public decimal Quantity {get; set;}
    public decimal? LimitPrice {get; set;}
    public OrderStatus Status {get; set;}
    public DateTime CreatedAtUtc {get;set;}
    public DateTime? ExecutedAtUtc {get;set;}


}
