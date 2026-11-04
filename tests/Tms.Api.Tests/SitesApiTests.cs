using System.Net;
using System.Net.Http.Json;
using Tms.Api.Contracts.Common;
using Tms.Api.Contracts.Sites;
using Tms.Domain.Enums;

namespace Tms.Api.Tests;

[Collection(ApiCollection.Name)]
public class SitesApiTests
{
    private readonly TmsApiFactory _factory;

    public SitesApiTests(TmsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_filters_by_country_and_returns_requested_page()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var page = await client.GetFromJsonAsync<PagedResult<SiteListItemDto>>(
            "/api/sites?country=DE&page=1&pageSize=5", TmsApiFactory.Json);

        Assert.NotNull(page);
        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, site => Assert.Equal("DE", site.CountryCode));
        Assert.True(page.Items.Count <= 5);
        Assert.True(page.TotalCount >= page.Items.Count);
    }

    [Fact]
    public async Task Free_text_search_matches_code_name_or_city()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var page = await client.GetFromJsonAsync<PagedResult<SiteListItemDto>>(
            "/api/sites?search=Hamburg", TmsApiFactory.Json);

        Assert.NotNull(page);
        Assert.Contains(page!.Items, site => site.SiteCode == "DE-HAM-0087");
    }

    [Fact]
    public async Task Status_filter_only_returns_that_status()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var page = await client.GetFromJsonAsync<PagedResult<SiteListItemDto>>(
            "/api/sites?status=Planned&pageSize=100", TmsApiFactory.Json);

        Assert.NotNull(page);
        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, site => Assert.Equal(SiteStatus.Planned, site.Status));
    }

    [Fact]
    public async Task Capacity_filter_splits_sites_into_with_and_without_free_slots()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var withCapacity = await client.GetFromJsonAsync<PagedResult<SiteListItemDto>>(
            "/api/sites?hasCapacity=true&pageSize=100", TmsApiFactory.Json);
        var withoutCapacity = await client.GetFromJsonAsync<PagedResult<SiteListItemDto>>(
            "/api/sites?hasCapacity=false&pageSize=100", TmsApiFactory.Json);

        Assert.NotNull(withCapacity);
        Assert.NotNull(withoutCapacity);
        Assert.All(withCapacity!.Items, site => Assert.True(site.AvailableSlots > 0));
        Assert.All(withoutCapacity!.Items, site => Assert.Equal(0, site.AvailableSlots));
        Assert.Contains(withoutCapacity.Items, site => site.SiteCode == "DE-HAM-0087");
    }

    [Fact]
    public async Task Site_detail_contains_towers_tenants_and_totals()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var page = await client.GetFromJsonAsync<PagedResult<SiteListItemDto>>(
            "/api/sites?search=DE-BER-0142", TmsApiFactory.Json);
        var siteId = Assert.Single(page!.Items).Id;

        var detail = await client.GetFromJsonAsync<SiteDetailDto>($"/api/sites/{siteId}", TmsApiFactory.Json);

        Assert.NotNull(detail);
        Assert.Equal("DE-BER-0142", detail!.SiteCode);
        Assert.Equal(2, detail.Towers.Count);
        Assert.Equal(detail.Totals.ActiveTenants, detail.Towers.Sum(t => t.ActiveTenants));
        Assert.Equal(detail.Totals.TenantCapacity, detail.Towers.Sum(t => t.TenantCapacity));
        Assert.NotEmpty(detail.Towers.SelectMany(t => t.Tenancies));
        Assert.True(detail.Totals.MonthlyRevenue > 0);
    }

    [Fact]
    public async Task Operations_can_register_a_site_with_its_first_tower()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);

        var created = await ApiTestHelpers.CreateSiteAsync(client, status: "Planned", tenantCapacity: 3);

        Assert.Equal("DE", created.CountryCode);
        var tower = Assert.Single(created.Towers);
        Assert.Equal(3, tower.TenantCapacity);
        Assert.True(tower.CanAcceptTenant);
        Assert.Equal(3, tower.AvailableSlots);
    }

    [Fact]
    public async Task Registering_the_same_site_code_twice_is_a_conflict()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);
        var first = await ApiTestHelpers.CreateSiteAsync(client);

        var duplicate = await client.PostAsJsonAsync("/api/sites", new
        {
            siteCode = first.SiteCode,
            name = "Duplicate",
            countryCode = "DE",
            city = "Berlin",
            address = "Other street 2",
            latitude = 52.5,
            longitude = 13.4,
            siteType = "Greenfield",
            status = "Planned"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("duplicate_site_code", await ApiTestHelpers.ReadProblemCodeAsync(duplicate));
    }

    [Fact]
    public async Task Invalid_payload_returns_a_validation_problem()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);

        var response = await client.PostAsJsonAsync("/api/sites", new
        {
            siteCode = "not-a-code",
            name = "Bad payload",
            countryCode = "DE",
            city = "Berlin",
            address = "Street 1",
            latitude = 200,
            longitude = 13.4,
            siteType = "Greenfield",
            status = "Planned"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Site_with_active_tenancies_cannot_be_decommissioned_or_deleted()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);

        var page = await client.GetFromJsonAsync<PagedResult<SiteListItemDto>>(
            "/api/sites?search=DE-BER-0142", TmsApiFactory.Json);
        var siteId = Assert.Single(page!.Items).Id;

        var decommission = await client.PatchAsJsonAsync(
            $"/api/sites/{siteId}/status", new { status = "Decommissioned" });

        Assert.Equal(HttpStatusCode.Conflict, decommission.StatusCode);
        Assert.Equal("site_has_active_tenancies", await ApiTestHelpers.ReadProblemCodeAsync(decommission));

        var delete = await client.DeleteAsync($"/api/sites/{siteId}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task Site_lifecycle_moves_forward_only()
    {
        using var client = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);
        var site = await ApiTestHelpers.CreateSiteAsync(client, status: "Planned");

        var activate = await client.PatchAsJsonAsync($"/api/sites/{site.Id}/status", new { status = "Active" });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var back = await client.PatchAsJsonAsync($"/api/sites/{site.Id}/status", new { status = "Planned" });
        Assert.Equal(HttpStatusCode.Conflict, back.StatusCode);
        Assert.Equal("invalid_site_status_transition", await ApiTestHelpers.ReadProblemCodeAsync(back));
    }
}
