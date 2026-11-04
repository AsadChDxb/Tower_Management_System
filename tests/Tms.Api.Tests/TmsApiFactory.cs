using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Tms.Api.Contracts.Auth;

namespace Tms.Api.Tests;

/// <summary>
/// Boots the real API pipeline against a throw-away LocalDB database that is migrated and
/// seeded on start-up and dropped on dispose.
/// </summary>
public class TmsApiFactory : WebApplicationFactory<Program>
{
    public const string DemoPassword = "TowerCo!2026";

    public const string AdminEmail = "admin@towerco.demo";
    public const string OperationsEmail = "ops@towerco.demo";
    public const string CommercialEmail = "commercial@towerco.demo";
    public const string ManagerEmail = "manager@towerco.demo";

    private const string MasterConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _databaseName = $"TmsTest_{Guid.NewGuid():N}";
    private readonly ConcurrentDictionary<string, string> _accessTokens = new();

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
                ["Jwt:SigningKey"] = "integration-test-signing-key-0123456789abcdef",
                ["Jwt:Issuer"] = "TowerManagementSystem.Tests",
                ["Jwt:Audience"] = "TowerManagementSystem.Tests.Client",
                ["Database:AutoMigrate"] = "true",
                ["Database:SeedDemoData"] = "true",
                ["Swagger:Enabled"] = "false",
                ["RateLimiting:AuthPermitLimit"] = "500"
            });
        });
    }

    public async Task<AuthResponse> LoginAsync(string email, string password = DemoPassword)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Login for {email} failed with {(int)response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<AuthResponse>(body, Json)
               ?? throw new InvalidOperationException("Login response could not be deserialized.");
    }

    /// <summary>Authenticated client for the given demo user (tokens are cached per user).</summary>
    public async Task<HttpClient> CreateClientAsAsync(string email)
    {
        if (!_accessTokens.TryGetValue(email, out var token))
        {
            token = (await LoginAsync(email)).AccessToken;
            _accessTokens[email] = token;
        }

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        SqlConnection.ClearAllPools();

        using var connection = new SqlConnection(MasterConnection);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID('{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END
            """;
        command.ExecuteNonQuery();
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<TmsApiFactory>
{
    public const string Name = "api";
}
