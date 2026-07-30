namespace OrderSimulatorApi;

public class Execution
{
    public int ExecutionId {get; set;}
    public int OrderId {get;set;}
    public decimal ExecutedPrice {get;set;}
    public decimal Quantity {get;set;}
    public DateTime ExecutedAtUtc {get;set;}
    
}