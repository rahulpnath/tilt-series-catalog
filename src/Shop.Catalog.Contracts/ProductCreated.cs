namespace Shop.Catalog.Contracts;

// Its own project because both sides need the same type: Rebus puts the type
// name on the wire.
public record ProductCreated(int Id, string Name, decimal Price);
