namespace Tms.Domain.Entities;

/// <summary>Base type that carries the audit timestamps every aggregate keeps.</summary>
public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
