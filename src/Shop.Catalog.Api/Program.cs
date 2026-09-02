using Shop.Catalog.Api;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// The catalog lives in memory for now. It gets a real database in Video 2,
// when there is somewhere to run one.
var products = new List<Product>
{
    new(1, "Espresso Machine", 749.00m),
    new(2, "Burr Grinder", 219.00m),
    new(3, "Milk Frother", 89.00m),
};
var lastId = products.Count;

app.MapGet("/", () => "Shop.Catalog API — hello from .NET 10");

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "catalog-api" }));

app.MapGet("/products", () => products);

app.MapPost("/products", (Product product) =>
{
    var created = product with { Id = ++lastId };
    products.Add(created);
    return Results.Created($"/products/{created.Id}", created);
});

app.Run();
