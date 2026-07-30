using MarketDataIngestion;
using RabbitMQ;
using Contracts.Events;
using Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ILivePricePublisher, LivePricePublisher>();
builder.Services.AddSingleton<IRabbitMQInfrastructure, RabbitMQInfrasctructure>();

var host = builder.Build();
host.Run();
