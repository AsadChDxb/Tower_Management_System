using Tms.Domain.Enums;

namespace Tms.Domain.Entities;

/// <summary>
/// A physical location in the portfolio (a plot of land, a roof, or an indoor space).
/// A site owns one or more towers; commercial and operational facts live here.
/// </summary>
public class Site : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human friendly business key, e.g. <c>DE-BER-0142</c>. Unique.</summary>
    public string SiteCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 country code, e.g. <c>DE</c>.</summary>
    public string CountryCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public SiteType SiteType { get; set; } = SiteType.Greenfield;

    public SiteStatus Status { get; set; } = SiteStatus.Planned;

    public DateOnly? AcquiredOn { get; set; }

    /// <summary>Ground lease / rent we pay for the site itself (not tenant revenue).</summary>
    public decimal? MonthlySiteCost { get; set; }

    public string? Notes { get; set; }

    public ICollection<Tower> Towers { get; set; } = new List<Tower>();
}
