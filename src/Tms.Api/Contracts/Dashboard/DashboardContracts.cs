namespace Tms.Api.Contracts.Dashboard;

public class DashboardKpiDto
{
    public int SiteCount { get; init; }

    public int ActiveSiteCount { get; init; }

    public int PlannedSiteCount { get; init; }

    public int DecommissionedSiteCount { get; init; }

    public int TowerCount { get; init; }

    public int TenantCapacity { get; init; }

    public int ActiveTenants { get; init; }

    public int AvailableSlots { get; init; }

    public int FullyUtilizedTowerCount { get; init; }

    public double UtilizationPercent { get; init; }

    public decimal MonthlyRevenue { get; init; }

    public decimal AverageRevenuePerTenant { get; init; }
}

public class CountrySummaryDto
{
    public string CountryCode { get; init; } = string.Empty;

    public int SiteCount { get; init; }

    public int TowerCount { get; init; }

    public int TenantCapacity { get; init; }

    public int ActiveTenants { get; init; }

    public int AvailableSlots { get; init; }

    public double UtilizationPercent { get; init; }

    public decimal MonthlyRevenue { get; init; }
}

public class StatusBreakdownDto
{
    public string Status { get; init; } = string.Empty;

    public int SiteCount { get; init; }
}

public class UtilizationBucketDto
{
    public string Label { get; init; } = string.Empty;

    public int TowerCount { get; init; }
}

public class OperatorRevenueDto
{
    public Guid OperatorId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string CountryCode { get; init; } = string.Empty;

    public int ActiveTenancies { get; init; }

    public decimal MonthlyRevenue { get; init; }

    public double RevenueSharePercent { get; init; }
}

/// <summary>Sales lead: a tower with spare slots, ordered by the most free capacity.</summary>
public class UnderUtilizedTowerDto
{
    public Guid TowerId { get; init; }

    public string TowerCode { get; init; } = string.Empty;

    public Guid SiteId { get; init; }

    public string SiteCode { get; init; } = string.Empty;

    public string SiteName { get; init; } = string.Empty;

    public string CountryCode { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public int TenantCapacity { get; init; }

    public int ActiveTenants { get; init; }

    public int AvailableSlots { get; init; }

    public double UtilizationPercent { get; init; }
}

public class MapSiteDto
{
    public Guid Id { get; init; }

    public string SiteCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string CountryCode { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public int TowerCount { get; init; }

    public int TenantCapacity { get; init; }

    public int ActiveTenants { get; init; }

    public double UtilizationPercent { get; init; }
}

public class DashboardSummaryDto
{
    public DateTimeOffset GeneratedAtUtc { get; init; }

    public string Currency { get; init; } = "EUR";

    public DashboardKpiDto Kpis { get; init; } = new();

    public IReadOnlyList<CountrySummaryDto> ByCountry { get; init; } = [];

    public IReadOnlyList<StatusBreakdownDto> ByStatus { get; init; } = [];

    public IReadOnlyList<UtilizationBucketDto> UtilizationBuckets { get; init; } = [];

    public IReadOnlyList<OperatorRevenueDto> TopOperatorsByRevenue { get; init; } = [];

    public IReadOnlyList<UnderUtilizedTowerDto> UnderUtilizedTowers { get; init; } = [];

    public IReadOnlyList<MapSiteDto> MapSites { get; init; } = [];
}
