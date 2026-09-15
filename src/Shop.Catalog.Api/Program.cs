using Microsoft.EntityFrameworkCore;
using Rebus.Bus;
using Rebus.Config;
using Shop.Catalog.Api;
using Shop.Catalog.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Db")));

// Publishes only, so no input queue of its own.
builder.Services.AddRebus(rebus => rebus
    .Transport(t => t.UseRabbitMqAsOneWayClient(builder.Configuration["Rabbit:Url"])));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

await DbBootstrap.EnsureCreatedAsync(app.Services, app.Logger);

app.MapGet("/", () => "Shop.Catalog API — hello from .NET 10");

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "catalog-api" }));

app.MapGet("/products", async (CatalogDbContext db) =>
    await db.Products.OrderByDescending(p => p.Id).ToListAsync());

app.MapGet("/products/{id:int}", async (int id, CatalogDbContext db) =>
    await db.Products.FindAsync(id) is { } product ? Results.Ok(product) : Results.NotFound());

app.MapPost("/products", async (Product product, CatalogDbContext db, IBus bus) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();

    await bus.Publish(new ProductCreated(product.Id, product.Name, product.Price));
    app.Logger.LogInformation("📤 published ProductCreated — #{Id} {Name}", product.Id, product.Name);

    return Results.Created($"/products/{product.Id}", product);
});

app.Run();
