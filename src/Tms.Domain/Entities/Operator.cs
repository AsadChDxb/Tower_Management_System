namespace Tms.Domain.Entities;

/// <summary>A mobile network operator (tenant company) that leases space on our towers.</summary>
public class Operator : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Company name, e.g. <c>NordTel</c>. Unique.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 home market, e.g. <c>DE</c>.</summary>
    public string CountryCode { get; set; } = string.Empty;

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public ICollection<Tenancy> Tenancies { get; set; } = new List<Tenancy>();
}
