namespace Tms.Infrastructure.Identity;

/// <summary>
/// Rotating refresh token. Only a SHA-256 hash of the token is stored, so a database
/// leak does not hand out usable sessions.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public AppUser? User { get; set; }

    /// <summary>Base64 SHA-256 hash of the raw token handed to the client.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string? CreatedByIp { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>Hash of the token that replaced this one during rotation (audit trail).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;
}
