using System.ComponentModel.DataAnnotations;
using Tms.Domain.Enums;

namespace Tms.Api.Contracts.Sites;

public class CreateSiteRequest
{
    [Required]
    [RegularExpression("^[A-Za-z]{2}-[A-Za-z]{3}-\\d{4}$",
        ErrorMessage = "Site code must follow the CC-CCC-NNNN pattern, e.g. DE-BER-0142.")]
    public string SiteCode { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    public SiteType SiteType { get; set; } = SiteType.Greenfield;

    public SiteStatus Status { get; set; } = SiteStatus.Planned;

    public DateOnly? AcquiredOn { get; set; }

    [Range(0, 1_000_000)]
    public decimal? MonthlySiteCost { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>Optional first tower created together with the site.</summary>
    public CreateTowerRequest? Tower { get; set; }
}

public class UpdateSiteRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    public SiteType SiteType { get; set; }

    public DateOnly? AcquiredOn { get; set; }

    [Range(0, 1_000_000)]
    public decimal? MonthlySiteCost { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class ChangeSiteStatusRequest
{
    [Required]
    public SiteStatus? Status { get; set; }
}

public class CreateTowerRequest
{
    [Required, MaxLength(40)]
    public string TowerCode { get; set; } = string.Empty;

    public TowerStructureType StructureType { get; set; } = TowerStructureType.Lattice;

    [Range(5, 250)]
    public double HeightMeters { get; set; }

    [Range(1, 50)]
    public int TenantCapacity { get; set; } = 3;

    [Range(0, 200)]
    public int AntennaMountCapacity { get; set; } = 8;

    public DateOnly? CommissionedOn { get; set; }
}

public class UpdateTowerRequest
{
    public TowerStructureType StructureType { get; set; }

    [Range(5, 250)]
    public double HeightMeters { get; set; }

    [Range(1, 50)]
    public int TenantCapacity { get; set; }

    [Range(0, 200)]
    public int AntennaMountCapacity { get; set; }

    public DateOnly? CommissionedOn { get; set; }
}
