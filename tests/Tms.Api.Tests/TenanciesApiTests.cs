using System.Net;
using System.Net.Http.Json;
using Tms.Api.Contracts.Common;
using Tms.Api.Contracts.Operators;
using Tms.Api.Contracts.Sites;
using Tms.Domain.Enums;

namespace Tms.Api.Tests;

[Collection(ApiCollection.Name)]
public class TenanciesApiTests
{
    private readonly TmsApiFactory _factory;

    public TenanciesApiTests(TmsApiFactory factory)
    {
        _factory = factory;
    }

    private static Task<HttpResponseMessage> AssignAsync(
        HttpClient client,
        Guid towerId,
        Guid operatorId,
        decimal rent = 1500m,
        string? startDate = null)
    {
        var body = new
        {
            towerId,
            operatorId,
            spaceType = "Antenna",
            antennaCount = 2,
            monthlyRent = rent,
            currency = "EUR",
            startDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        };

        return client.PostAsJsonAsync("/api/tenancies", body);
    }

    private static async Task<List<OperatorDto>> OperatorsAsync(HttpClient client) =>
        await client.GetFromJsonAsync<List<OperatorDto>>("/api/operators", TmsApiFactory.Json) ?? [];

    [Fact]
    public async Task Assigning_a_tenant_to_a_full_tower_is_a_conflict()
    {
        using var operations = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);
        using var commercial = await _factory.CreateClientAsAsync(TmsApiFactory.CommercialEmail);

        var site = await ApiTestHelpers.CreateSiteAsync(operations, status: "Active", tenantCapacity: 1);
        var towerId = site.Towers[0].Id;
        var operators = await OperatorsAsync(commercial);

        Assert.Equal(HttpStatusCode.Created, (await AssignAsync(commercial, towerId, operators[0].Id)).StatusCode);

        var second = await AssignAsync(commercial, towerId, operators[1].Id);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("tower_capacity_exceeded", await ApiTestHelpers.ReadProblemCodeAsync(second));
    }

    [Fact]
    public async Task The_same_operator_cannot_lease_the_same_tower_twice()
    {
        using var operations = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);
        using var commercial = await _factory.CreateClientAsAsync(TmsApiFactory.CommercialEmail);

        var site = await ApiTestHelpers.CreateSiteAsync(operations, status: "Active", tenantCapacity: 3);
        var towerId = site.Towers[0].Id;
        var operatorId = (await OperatorsAsync(commercial))[0].Id;

        Assert.Equal(HttpStatusCode.Created, (await AssignAsync(commercial, towerId, operatorId)).StatusCode);

        var duplicate = await AssignAsync(commercial, towerId, operatorId);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("duplicate_active_tenancy", await ApiTestHelpers.ReadProblemCodeAsync(duplicate));
    }

    [Fact]
    public async Task A_planned_site_cannot_be_leased()
    {
        using var operations = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);
        using var commercial = await _factory.CreateClientAsAsync(TmsApiFactory.CommercialEmail);

        var site = await ApiTestHelpers.CreateSiteAsync(operations, status: "Planned", tenantCapacity: 2);
        var operatorId = (await OperatorsAsync(commercial))[0].Id;

        var response = await AssignAsync(commercial, site.Towers[0].Id, operatorId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("site_not_leasable", await ApiTestHelpers.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Operations_role_cannot_assign_tenants()
    {
        using var operations = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);
        using var commercial = await _factory.CreateClientAsAsync(TmsApiFactory.CommercialEmail);

        var site = await ApiTestHelpers.CreateSiteAsync(operations, status: "Active", tenantCapacity: 2);
        var operatorId = (await OperatorsAsync(commercial))[0].Id;

        var response = await AssignAsync(operations, site.Towers[0].Id, operatorId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assigning_a_tenant_uses_the_last_slot_and_terminating_frees_it()
    {
        using var operations = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);
        using var commercial = await _factory.CreateClientAsAsync(TmsApiFactory.CommercialEmail);

        var site = await ApiTestHelpers.CreateSiteAsync(operations, status: "Active", tenantCapacity: 1);
        var towerId = site.Towers[0].Id;
        var operatorId = (await OperatorsAsync(commercial))[2].Id;

        var capacityBefore = await commercial.GetFromJsonAsync<TowerCapacityDto>(
            $"/api/towers/{towerId}/capacity", TmsApiFactory.Json);
        Assert.True(capacityBefore!.CanAcceptTenant);

        var created = await AssignAsync(commercial, towerId, operatorId);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var tenancy = await created.Content.ReadFromJsonAsync<TenancyDto>(TmsApiFactory.Json);

        var capacityAfter = await commercial.GetFromJsonAsync<TowerCapacityDto>(
            $"/api/towers/{towerId}/capacity", TmsApiFactory.Json);
        Assert.False(capacityAfter!.CanAcceptTenant);
        Assert.Equal(0, capacityAfter.AvailableSlots);
        Assert.Equal(100, capacityAfter.UtilizationPercent, 1);

        var terminate = await commercial.PostAsJsonAsync(
            $"/api/tenancies/{tenancy!.Id}/terminate",
            new { endDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd") });
        Assert.Equal(HttpStatusCode.OK, terminate.StatusCode);

        var capacityFreed = await commercial.GetFromJsonAsync<TowerCapacityDto>(
            $"/api/towers/{towerId}/capacity", TmsApiFactory.Json);
        Assert.True(capacityFreed!.CanAcceptTenant);
        Assert.Equal(1, capacityFreed.AvailableSlots);
    }

    [Fact]
    public async Task Invalid_commercial_terms_are_rejected_with_400()
    {
        using var operations = await _factory.CreateClientAsAsync(TmsApiFactory.OperationsEmail);
        using var commercial = await _factory.CreateClientAsAsync(TmsApiFactory.CommercialEmail);

        var site = await ApiTestHelpers.CreateSiteAsync(operations, status: "Active", tenantCapacity: 2);
        var operatorId = (await OperatorsAsync(commercial))[0].Id;

        var zeroRent = await AssignAsync(commercial, site.Towers[0].Id, operatorId, rent: 0m);
        Assert.Equal(HttpStatusCode.BadRequest, zeroRent.StatusCode);

        var badDates = await commercial.PostAsJsonAsync("/api/tenancies", new
        {
            towerId = site.Towers[0].Id,
            operatorId,
            spaceType = "Antenna",
            antennaCount = 1,
            monthlyRent = 1200,
            currency = "EUR",
            startDate = "2026-05-10",
            endDate = "2026-05-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, badDates.StatusCode);
        Assert.Equal("invalid_tenancy_term", await ApiTestHelpers.ReadProblemCodeAsync(badDates));
    }

    [Fact]
    public async Task Tenancy_list_can_be_filtered_by_status()
    {
        using var manager = await _factory.CreateClientAsAsync(TmsApiFactory.ManagerEmail);

        var active = await manager.GetFromJsonAsync<PagedResult<TenancyDto>>(
            "/api/tenancies?status=Active&pageSize=100", TmsApiFactory.Json);

        Assert.NotNull(active);
        Assert.NotEmpty(active!.Items);
        Assert.All(active.Items, tenancy => Assert.Equal(TenancyStatus.Active, tenancy.Status));
        Assert.All(active.Items, tenancy => Assert.False(string.IsNullOrWhiteSpace(tenancy.OperatorName)));
    }
}
