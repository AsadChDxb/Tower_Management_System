using Tms.Domain.Enums;

namespace Tms.Domain.Entities;

/// <summary>
/// The leasable asset on a site. <see cref="TenantCapacity"/> is the maximum number of
/// operators that can be active on the tower at the same time.
/// </summary>
public class Tower : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SiteId { get; set; }

    public Site? Site { get; set; }

    /// <summary>Human friendly business key, e.g. <c>DE-BER-0142-T1</c>. Unique.</summary>
    public string TowerCode { get; set; } = string.Empty;

    public TowerStructureType StructureType { get; set; } = TowerStructureType.Lattice;

    public double HeightMeters { get; set; }

    /// <summary>Maximum number of concurrent tenants (operator slots) on this tower.</summary>
    public int TenantCapacity { get; set; }

    /// <summary>Physical antenna mount positions available.</summary>
    public int AntennaMountCapacity { get; set; }

    public DateOnly? CommissionedOn { get; set; }

    public ICollection<Tenancy> Tenancies { get; set; } = new List<Tenancy>();
}
