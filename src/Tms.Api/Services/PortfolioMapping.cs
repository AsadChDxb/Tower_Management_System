using Tms.Api.Contracts.Sites;
using Tms.Domain.Entities;
using Tms.Domain.Enums;
using Tms.Domain.Portfolio;

namespace Tms.Api.Services;

/// <summary>Explicit entity -> DTO mapping (no AutoMapper: easy to read and debug live).</summary>
public static class PortfolioMapping
{
    public static SiteDetailDto ToDetailDto(Site site)
    {
        var towers = site.Towers
            .OrderBy(t => t.TowerCode, StringComparer.Ordinal)
            .Select(ToTowerDto)
            .ToList();

        var tenantCapacity = towers.Sum(t => t.TenantCapacity);
        var activeTenants = towers.Sum(t => t.ActiveTenants);
        var capacity = new TowerCapacity(tenantCapacity, activeTenants);

        return new SiteDetailDto
        {
            Id = site.Id,
            SiteCode = site.SiteCode,
            Name = site.Name,
            CountryCode = site.CountryCode,
            City = site.City,
            Address = site.Address,
            Latitude = site.Latitude,
            Longitude = site.Longitude,
            SiteType = site.SiteType,
            Status = site.Status,
            AcquiredOn = site.AcquiredOn,
            MonthlySiteCost = site.MonthlySiteCost,
            Notes = site.Notes,
            CreatedAtUtc = site.CreatedAtUtc,
            UpdatedAtUtc = site.UpdatedAtUtc,
            Totals = new SitePortfolioTotalsDto
            {
                TowerCount = towers.Count,
                TenantCapacity = tenantCapacity,
                ActiveTenants = activeTenants,
                AvailableSlots = capacity.AvailableSlots,
                UtilizationPercent = capacity.UtilizationPercent,
                MonthlyRevenue = towers.SelectMany(t => t.Tenancies)
                    .Where(t => t.Status == TenancyStatus.Active)
                    .Sum(t => t.MonthlyRent)
            },
            Towers = towers
        };
    }

    public static TowerDto ToTowerDto(Tower tower)
    {
        var activeTenants = tower.Tenancies.Count(t => t.Status == TenancyStatus.Active);
        var capacity = new TowerCapacity(tower.TenantCapacity, activeTenants);

        return new TowerDto
        {
            Id = tower.Id,
            TowerCode = tower.TowerCode,
            StructureType = tower.StructureType,
            HeightMeters = tower.HeightMeters,
            TenantCapacity = tower.TenantCapacity,
            AntennaMountCapacity = tower.AntennaMountCapacity,
            CommissionedOn = tower.CommissionedOn,
            ActiveTenants = activeTenants,
            AvailableSlots = capacity.AvailableSlots,
            UtilizationPercent = capacity.UtilizationPercent,
            CanAcceptTenant = capacity.CanAcceptTenant,
            Tenancies = tower.Tenancies
                .OrderByDescending(t => t.Status == TenancyStatus.Active)
                .ThenByDescending(t => t.StartDate)
                .Select(ToTenancyDto)
                .ToList()
        };
    }

    public static TenancyDto ToTenancyDto(Tenancy tenancy)
    {
        var tower = tenancy.Tower;
        var site = tower?.Site;

        return new TenancyDto
        {
            Id = tenancy.Id,
            TowerId = tenancy.TowerId,
            TowerCode = tower?.TowerCode ?? string.Empty,
            SiteId = site?.Id ?? Guid.Empty,
            SiteCode = site?.SiteCode ?? string.Empty,
            SiteName = site?.Name ?? string.Empty,
            OperatorId = tenancy.OperatorId,
            OperatorName = tenancy.Operator?.Name ?? string.Empty,
            SpaceType = tenancy.SpaceType,
            AntennaCount = tenancy.AntennaCount,
            MonthlyRent = tenancy.MonthlyRent,
            Currency = tenancy.Currency,
            StartDate = tenancy.StartDate,
            EndDate = tenancy.EndDate,
            Status = tenancy.Status,
            Notes = tenancy.Notes,
            CreatedAtUtc = tenancy.CreatedAtUtc
        };
    }

    public static TowerCapacityDto ToCapacityDto(Tower tower, int activeTenants)
    {
        var capacity = new TowerCapacity(tower.TenantCapacity, activeTenants);

        return new TowerCapacityDto
        {
            TowerId = tower.Id,
            TowerCode = tower.TowerCode,
            SiteCode = tower.Site?.SiteCode ?? string.Empty,
            TenantCapacity = capacity.TenantCapacity,
            ActiveTenants = capacity.ActiveTenants,
            AvailableSlots = capacity.AvailableSlots,
            UtilizationPercent = capacity.UtilizationPercent,
            CanAcceptTenant = capacity.CanAcceptTenant,
            Summary = capacity.CanAcceptTenant
                ? $"Tower {tower.TowerCode} has {capacity.AvailableSlots} free tenant slot(s) ({capacity.UtilizationPercent:0.#}% utilized)."
                : $"Tower {tower.TowerCode} is full ({capacity.ActiveTenants}/{capacity.TenantCapacity} slots used)."
        };
    }
}
