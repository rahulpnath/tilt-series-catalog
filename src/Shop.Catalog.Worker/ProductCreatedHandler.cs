using Rebus.Handlers;
using Shop.Catalog.Contracts;

namespace Shop.Catalog.Worker;

// One method, one message type — Rebus does the queue plumbing.
public class ProductCreatedHandler(ILogger<ProductCreatedHandler> logger)
    : IHandleMessages<ProductCreated>
{
    public Task Handle(ProductCreated message)
    {
        logger.LogInformation("product.created — #{Id} {Name} at {Price}",
            message.Id, message.Name, message.Price);
        return Task.CompletedTask;
    }
}
