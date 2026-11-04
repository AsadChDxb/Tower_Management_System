using System.Security.Claims;

namespace Tms.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(raw, out var userId))
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user identifier.");
        }

        return userId;
    }
}
