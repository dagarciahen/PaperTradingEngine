using MarketDataIngestion;
using Infrastructure;
using Contracts.Interfaces;
using Contracts.Events;



var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BinanceSettings>(
    builder.Configuration.GetSection(BinanceSettings.SectionName));

builder.Services.AddSingleton<ILivePricePublisher, LivePricePublisher>();
builder.Services.AddSingleton<IRabbitMQInfrastructure, RabbitMQInfrasctructure>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddOptions<RabbitMQSettings>().Bind(
    builder.Configuration.GetSection(RabbitMQSettings.SectionName))
    .Validate(s => !string.IsNullOrEmpty(s.HostName), "RabbitMQ:HostName missing")
    .Validate(s => !string.IsNullOrEmpty(s.UserName), "RabbitMQ:UserName missing")
    .Validate(s => !string.IsNullOrEmpty(s.Password), "RabbitMq:Password missing")
    .Validate(s => !string.IsNullOrEmpty(s.LivePricesExchangeName), "RabbitMQ:LivePricesExchangeName missing")
    .Validate(s => !string.IsNullOrEmpty(s.ExecutionQueueName), "RabbitMQ:ExecutionQueueName missing")
    .Validate(s => !string.IsNullOrEmpty(s.ReportingQueueName), "RabbitMQ:ReportingQueueName missing")
    .ValidateOnStart();


var host = builder.Build();
host.Run();
