using Tms.Domain.Enums;

namespace Tms.Api.Contracts.Sites;

public class SitePortfolioTotalsDto
{
    public int TowerCount { get; init; }

    public int TenantCapacity { get; init; }

    public int ActiveTenants { get; init; }

    public int AvailableSlots { get; init; }

    public double UtilizationPercent { get; init; }

    public decimal MonthlyRevenue { get; init; }

    public string Currency { get; init; } = "EUR";
}

/// <summary>Everything about one site - the "single view" of use case 3.</summary>
public class SiteDetailDto
{
    public Guid Id { get; init; }

    public string SiteCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string CountryCode { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public SiteType SiteType { get; init; }

    public SiteStatus Status { get; init; }

    public DateOnly? AcquiredOn { get; init; }

    public decimal? MonthlySiteCost { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }

    public SitePortfolioTotalsDto Totals { get; init; } = new();

    public IReadOnlyList<TowerDto> Towers { get; init; } = [];
}
