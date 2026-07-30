using Contracts.Domain.V1;

namespace OrderSimulatorApi;

public class CreateOrderRequest
{
	public int UserId {get;set;}
	public string Symbol {get; set;} = string.Empty;
	public OrderType Type {get; set;} 
	public decimal Quantity {get; set;}
	public decimal? LimitPrice {get;set;}
}

