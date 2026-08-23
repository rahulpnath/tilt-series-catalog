using Microsoft.EntityFrameworkCore;

namespace Shop.Catalog.Api;

// SQL Server takes ~20s to accept connections. Demo only: EnsureCreated
// stands in for migrations.
public static class DbBootstrap
{
    public static async Task EnsureCreatedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        for (var attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                logger.LogInformation("Catalog database ready (attempt {Attempt}).", attempt);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning("SQL not ready yet (attempt {Attempt}/30): {Message}",
                    attempt, ex.Message);
                await Task.Delay(2000);
            }
        }

        throw new InvalidOperationException("Catalog database never became ready.");
    }
}
