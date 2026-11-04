using Microsoft.AspNetCore.Identity;

namespace Tms.Infrastructure.Identity;

/// <summary>
/// Application user. We keep ASP.NET Core Identity for password hashing, lockout and
/// unique e-mail enforcement, but issue our own JWTs (see <c>JwtTokenService</c>).
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
        Id = Guid.NewGuid();
    }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Soft disable - a deactivated user can no longer log in.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
