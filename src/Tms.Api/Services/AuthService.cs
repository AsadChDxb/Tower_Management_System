using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tms.Api.Auth;
using Tms.Api.Contracts.Auth;
using Tms.Infrastructure.Identity;
using Tms.Infrastructure.Persistence;

namespace Tms.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress);

    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress);

    Task LogoutAsync(string refreshToken);

    Task<UserDto> GetCurrentUserAsync(Guid userId);
}

/// <summary>
/// Issues our own JWTs backed by ASP.NET Core Identity users, with rotating refresh tokens.
/// Access tokens are short lived (15 min by default); refresh tokens are stored hashed only.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly TmsDbContext _db;
    private readonly JwtOptions _jwt;

    public AuthService(UserManager<AppUser> userManager, TmsDbContext db, IOptions<JwtOptions> jwt)
    {
        _userManager = userManager;
        _db = db;
        _jwt = jwt.Value;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.IsActive)
        {
            // Same message for "unknown user" and "wrong password" - no account enumeration.
            throw new UnauthorizedAccessException("Invalid e-mail or password.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            throw new UnauthorizedAccessException("Invalid e-mail or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return await IssueTokensAsync(user, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress)
    {
        var hash = HashToken(refreshToken);

        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null || !stored.IsActive || stored.User is null || !stored.User.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid, revoked or expired. Please sign in again.");
        }

        var issued = await IssueTokensAsync(stored.User, ipAddress);

        stored.RevokedAtUtc = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenHash = HashToken(issued.RefreshToken);
        await _db.SaveChangesAsync();

        return issued;
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAtUtc == null);

        if (stored is null)
        {
            return;
        }

        stored.RevokedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
                   ?? throw new KeyNotFoundException("User not found.");

        return await ToDtoAsync(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(AppUser user, string? ipAddress)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = Permissions.ForRoles(roles);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = credentials
        };

        var accessToken = new JwtSecurityTokenHandler().CreateToken(descriptor);
        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        var dto = new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.IsActive,
            roles.ToList(),
            permissions,
            user.CreatedAtUtc);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(accessToken),
            expiresAt,
            rawRefreshToken,
            refreshToken.ExpiresAtUtc,
            dto);
    }

    private async Task<UserDto> ToDtoAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.IsActive,
            roles.ToList(),
            Permissions.ForRoles(roles),
            user.CreatedAtUtc);
    }

    private static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
