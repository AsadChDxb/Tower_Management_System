using Tms.Domain.Enums;

namespace Tms.Api.Contracts.Sites;

/// <summary>Query string filters for the site search (use case 3).</summary>
public class SiteQuery : Common.PagedQuery
{
    /// <summary>Free text over site code, name and city.</summary>
    public string? Search { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code.</summary>
    public string? Country { get; set; }

    public SiteStatus? Status { get; set; }

    /// <summary>true = only sites with a free tenant slot, false = only fully utilized / over-committed sites.</summary>
    public bool? HasCapacity { get; set; }

    /// <summary>code (default) | name | country | utilization | revenue</summary>
    public string? Sort { get; set; }
}

public class SiteListItemDto
{
    public Guid Id { get; init; }

    public string SiteCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string CountryCode { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public SiteStatus Status { get; init; }

    public SiteType SiteType { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public int TowerCount { get; init; }

    public int TenantCapacity { get; init; }

    public int ActiveTenants { get; init; }

    public int AvailableSlots { get; init; }

    public double UtilizationPercent { get; init; }

    public decimal MonthlyRevenue { get; init; }

    public string Currency { get; init; } = "EUR";

    public DateTimeOffset UpdatedAtUtc { get; init; }
}
