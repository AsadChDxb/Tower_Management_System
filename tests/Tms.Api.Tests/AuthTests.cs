using System.Net;
using System.Net.Http.Json;
using Tms.Api.Contracts.Auth;

namespace Tms.Api.Tests;

[Collection(ApiCollection.Name)]
public class AuthTests
{
    private readonly TmsApiFactory _factory;

    public AuthTests(TmsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_returns_token_roles_and_permissions()
    {
        var auth = await _factory.LoginAsync(TmsApiFactory.CommercialEmail);

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Contains("Commercial", auth.User.Roles);
        Assert.Contains("Tenancies.Manage", auth.User.Permissions);
        Assert.DoesNotContain("Sites.Manage", auth.User.Permissions);
        Assert.True(auth.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.True(auth.RefreshTokenExpiresAtUtc > auth.AccessTokenExpiresAtUtc);
    }

    [Fact]
    public async Task Wrong_password_returns_401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TmsApiFactory.AdminEmail, password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_without_a_token_returns_401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/sites");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_caller_with_permissions()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me", TmsApiFactory.Json);

        Assert.NotNull(me);
        Assert.Contains("Manager", me!.Roles);
        Assert.Contains("Dashboard.View", me.Permissions);
        Assert.DoesNotContain("Users.Manage", me.Permissions);
    }

    [Fact]
    public async Task Refresh_rotates_the_refresh_token_and_rejects_the_replayed_one()
    {
        var first = await _factory.LoginAsync(TmsApiFactory.OperationsEmail);
        using var client = _factory.CreateClient();

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        refreshResponse.EnsureSuccessStatusCode();
        var second = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(TmsApiFactory.Json);

        Assert.NotNull(second);
        Assert.NotEqual(first.RefreshToken, second!.RefreshToken);

        var replay = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Manager_can_read_the_portfolio_but_cannot_manage_it()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/sites")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users")).StatusCode);

        var create = await client.PostAsJsonAsync("/api/sites", new
        {
            siteCode = "DE-NOP-0001",
            name = "Should not be created",
            countryCode = "DE",
            city = "Berlin",
            address = "Nowhere 1",
            latitude = 52.5,
            longitude = 13.4,
            siteType = "Greenfield",
            status = "Planned"
        });

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Admin_can_manage_users_and_create_a_role_scoped_account()
    {
        using var admin = await _factory.CreateClientAsAsync(TmsApiFactory.AdminEmail);

        var users = await admin.GetFromJsonAsync<List<UserDto>>("/api/users", TmsApiFactory.Json);
        Assert.NotNull(users);
        Assert.Contains(users!, user => user.Email == TmsApiFactory.AdminEmail);

        var email = $"tester.{Guid.NewGuid():N}@towerco.demo";
        var create = await admin.PostAsJsonAsync("/api/users", new
        {
            email,
            displayName = "Integration Tester",
            password = "TowerCo!2026",
            roles = new[] { "Operations" }
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<UserDto>(TmsApiFactory.Json);
        Assert.NotNull(created);
        Assert.Contains("Operations", created!.Roles);
        Assert.Contains("Sites.Manage", created.Permissions);
    }
}
