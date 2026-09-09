using Microsoft.EntityFrameworkCore;

namespace Shop.Catalog.Api;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    // Without an explicit precision SQL Server truncates the decimal silently.
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
}
