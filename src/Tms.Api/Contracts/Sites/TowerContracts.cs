using Tms.Domain.Enums;

namespace Tms.Api.Contracts.Sites;

public class TowerDto
{
    public Guid Id { get; init; }

    public string TowerCode { get; init; } = string.Empty;

    public TowerStructureType StructureType { get; init; }

    public double HeightMeters { get; init; }

    public int TenantCapacity { get; init; }

    public int AntennaMountCapacity { get; init; }

    public DateOnly? CommissionedOn { get; init; }

    public int ActiveTenants { get; init; }

    public int AvailableSlots { get; init; }

    public double UtilizationPercent { get; init; }

    public bool CanAcceptTenant { get; init; }

    public IReadOnlyList<TenancyDto> Tenancies { get; init; } = [];
}

public class TenancyDto
{
    public Guid Id { get; init; }

    public Guid TowerId { get; init; }

    public string TowerCode { get; init; } = string.Empty;

    public Guid SiteId { get; init; }

    public string SiteCode { get; init; } = string.Empty;

    public string SiteName { get; init; } = string.Empty;

    public Guid OperatorId { get; init; }

    public string OperatorName { get; init; } = string.Empty;

    public SpaceType SpaceType { get; init; }

    public int AntennaCount { get; init; }

    public decimal MonthlyRent { get; init; }

    public string Currency { get; init; } = "EUR";

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public TenancyStatus Status { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}

/// <summary>Answer to "does this tower have room for one more tenant?" (use case 2).</summary>
public class TowerCapacityDto
{
    public Guid TowerId { get; init; }

    public string TowerCode { get; init; } = string.Empty;

    public string SiteCode { get; init; } = string.Empty;

    public int TenantCapacity { get; init; }

    public int ActiveTenants { get; init; }

    public int AvailableSlots { get; init; }

    public double UtilizationPercent { get; init; }

    public bool CanAcceptTenant { get; init; }

    public string Summary { get; init; } = string.Empty;
}
