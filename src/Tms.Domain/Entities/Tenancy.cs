using Tms.Domain.Enums;

namespace Tms.Domain.Entities;

/// <summary>
/// A lease agreement: one operator occupying space on one tower.
/// Active tenancies consume a tenant slot on the tower, which is what drives capacity.
/// </summary>
public class Tenancy : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TowerId { get; set; }

    public Tower? Tower { get; set; }

    public Guid OperatorId { get; set; }

    public Operator? Operator { get; set; }

    public SpaceType SpaceType { get; set; } = SpaceType.Antenna;

    public int AntennaCount { get; set; } = 1;

    /// <summary>Monthly recurring revenue from this tenancy.</summary>
    public decimal MonthlyRent { get; set; }

    /// <summary>ISO 4217 currency code, e.g. <c>EUR</c>.</summary>
    public string Currency { get; set; } = "EUR";

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public TenancyStatus Status { get; set; } = TenancyStatus.Active;

    public string? Notes { get; set; }
}
