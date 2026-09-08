using Microsoft.EntityFrameworkCore;

namespace Shop.Catalog.Api;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}
