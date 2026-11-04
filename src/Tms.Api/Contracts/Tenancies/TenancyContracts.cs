using System.ComponentModel.DataAnnotations;
using Tms.Domain.Enums;

namespace Tms.Api.Contracts.Tenancies;

public class CreateTenancyRequest
{
    [Required]
    public Guid TowerId { get; set; }

    [Required]
    public Guid OperatorId { get; set; }

    public SpaceType SpaceType { get; set; } = SpaceType.Antenna;

    [Range(0, 100)]
    public int AntennaCount { get; set; } = 1;

    [Range(0.01, 10_000_000)]
    public decimal MonthlyRent { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "EUR";

    [Required]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly? EndDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateTenancyRequest
{
    public SpaceType SpaceType { get; set; }

    [Range(0, 100)]
    public int AntennaCount { get; set; }

    [Range(0.01, 10_000_000)]
    public decimal MonthlyRent { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "EUR";

    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class TerminateTenancyRequest
{
    /// <summary>Defaults to today when omitted.</summary>
    public DateOnly? EndDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class TenancyQuery : Common.PagedQuery
{
    public Guid? TowerId { get; set; }

    public Guid? OperatorId { get; set; }

    public TenancyStatus? Status { get; set; }

    public string? Country { get; set; }

    public string? Search { get; set; }
}
