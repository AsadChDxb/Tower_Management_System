namespace Tms.Api.Auth;

/// <summary>JWT settings. In Azure these come from app settings / Key Vault, never from source.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TowerManagementSystem";

    public string Audience { get; set; } = "TowerManagementSystem.Client";

    /// <summary>HMAC-SHA256 signing key (minimum 32 characters). Override in production.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}
