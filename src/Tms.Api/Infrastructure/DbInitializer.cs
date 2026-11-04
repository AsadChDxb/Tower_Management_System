using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tms.Infrastructure.Identity;
using Tms.Infrastructure.Persistence;
using Tms.Infrastructure.Persistence.Seed;

namespace Tms.Api.Infrastructure;

/// <summary>
/// Migrates and seeds at startup (dev/demo convenience). In production the migration is
/// usually applied from a deployment step - see docs/azure-deployment.md.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var db = provider.GetRequiredService<TmsDbContext>();

        if (configuration.GetValue("Database:AutoMigrate", true))
        {
            logger.LogInformation("Applying EF Core migrations...");
            await db.Database.MigrateAsync();
        }

        if (!configuration.GetValue("Database:SeedDemoData", true))
        {
            return;
        }

        await IdentitySeeder.SeedAsync(
            provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            provider.GetRequiredService<UserManager<AppUser>>());

        await DemoDataSeeder.SeedAsync(db);

        logger.LogInformation("Identity and demo portfolio are in place.");
    }
}
