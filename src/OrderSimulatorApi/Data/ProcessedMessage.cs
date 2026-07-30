namespace OrderSimulatorApi;

public class ProcessedMessage
{
    public string MessageId {get;set;} = string.Empty;
    public DateTime ProcessedAtUtc {get;set;}
}

