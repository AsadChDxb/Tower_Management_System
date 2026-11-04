using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tms.Api.Auth;
using Tms.Api.Contracts.Auth;
using Tms.Api.Services;

namespace Tms.Api.Controllers;

/// <summary>Authentication: JWT login, refresh-token rotation, logout and "who am I".</summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Signs a user in and returns a JWT access token plus a rotating refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await _auth.LoginAsync(request, ClientIp));

    /// <summary>Exchanges a valid refresh token for a fresh access/refresh pair (rotation).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        Ok(await _auth.RefreshAsync(request.RefreshToken, ClientIp));

    /// <summary>Revokes the supplied refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _auth.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    /// <summary>Current user, roles and permissions - drives the role-based menu in the SPA.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await _auth.GetCurrentUserAsync(User.GetUserId()));

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
}
