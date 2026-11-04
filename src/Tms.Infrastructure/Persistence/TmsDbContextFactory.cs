using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tms.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> at design time so migrations can be created without booting the API.
/// Order of precedence: TMS_CONNECTION env var, then the local development database.
/// </summary>
public class TmsDbContextFactory : IDesignTimeDbContextFactory<TmsDbContext>
{
    private const string LocalDbConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=TowerManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public TmsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TMS_CONNECTION") ?? LocalDbConnectionString;

        var options = new DbContextOptionsBuilder<TmsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TmsDbContext(options);
    }
}
