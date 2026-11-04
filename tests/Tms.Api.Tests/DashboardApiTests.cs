using System.Net;
using System.Net.Http.Json;
using Tms.Api.Contracts.Dashboard;

namespace Tms.Api.Tests;

[Collection(ApiCollection.Name)]
public class DashboardApiTests
{
    private readonly TmsApiFactory _factory;

    public DashboardApiTests(TmsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_requires_authentication()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Summary_numbers_are_internally_consistent()
    {
        using var manager = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var summary = await manager.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", TmsApiFactory.Json);

        Assert.NotNull(summary);
        var kpis = summary!.Kpis;

        Assert.True(kpis.SiteCount > 0);
        Assert.True(kpis.TowerCount > 0);
        Assert.True(kpis.MonthlyRevenue > 0);
        Assert.Equal(kpis.ActiveSiteCount + kpis.PlannedSiteCount + kpis.DecommissionedSiteCount, kpis.SiteCount);

        Assert.Equal(kpis.SiteCount, summary.ByStatus.Sum(status => status.SiteCount));
        Assert.Equal(kpis.SiteCount, summary.MapSites.Count);
        Assert.Equal(kpis.TowerCount, summary.ByCountry.Sum(country => country.TowerCount));
        Assert.Equal(kpis.TenantCapacity, summary.ByCountry.Sum(country => country.TenantCapacity));
        Assert.Equal(kpis.ActiveTenants, summary.ByCountry.Sum(country => country.ActiveTenants));
        Assert.Equal(kpis.TowerCount, summary.UtilizationBuckets.Sum(bucket => bucket.TowerCount));
        Assert.Equal(kpis.TenantCapacity - kpis.ActiveTenants, kpis.AvailableSlots);
    }

    [Fact]
    public async Task Summary_highlights_under_utilized_towers_and_top_operators()
    {
        using var manager = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var summary = await manager.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", TmsApiFactory.Json);

        Assert.NotNull(summary);
        Assert.NotEmpty(summary!.UnderUtilizedTowers);
        Assert.All(summary.UnderUtilizedTowers, tower => Assert.True(tower.AvailableSlots > 0));
        Assert.All(summary.UnderUtilizedTowers, tower => Assert.False(string.IsNullOrWhiteSpace(tower.SiteCode)));

        Assert.NotEmpty(summary.TopOperatorsByRevenue);
        Assert.True(summary.TopOperatorsByRevenue.Count <= 5);
        Assert.All(summary.TopOperatorsByRevenue, row => Assert.InRange(row.RevenueSharePercent, 0, 100));
    }

    [Fact]
    public async Task Map_pins_carry_coordinates_and_utilization()
    {
        using var manager = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var summary = await manager.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", TmsApiFactory.Json);

        Assert.NotNull(summary);
        Assert.All(summary!.MapSites, site =>
        {
            Assert.InRange(site.Latitude, -90, 90);
            Assert.InRange(site.Longitude, -180, 180);
            Assert.False(string.IsNullOrWhiteSpace(site.CountryCode));
            Assert.InRange(site.UtilizationPercent, 0, 200);
        });
    }
}
