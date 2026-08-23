using Rebus.Config;
using Shop.Catalog.Contracts;
using Shop.Catalog.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRebus(
    rebus => rebus.Transport(t =>
        t.UseRabbitMq(builder.Configuration["Rabbit:Url"], "catalog-worker")),
    onCreated: async bus => await bus.Subscribe<ProductCreated>());

builder.Services.AddRebusHandler<ProductCreatedHandler>();

var host = builder.Build();
host.Run();
