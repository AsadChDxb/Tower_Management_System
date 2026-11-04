using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tms.Api.Auth;
using Tms.Infrastructure.Persistence;
using Tms.Infrastructure.Persistence.Seed;

namespace Tms.Api.Controllers;

/// <summary>Demo helpers - only registered in the Development environment.</summary>
[ApiController]
[Route("api/dev")]
[Authorize(Policy = Permissions.UsersManage)]
[Tags("Dev")]
public class DevController : ControllerBase
{
    private readonly TmsDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public DevController(TmsDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    /// <summary>Resets the portfolio to the seeded demo data (users are untouched).</summary>
    [HttpPost("reset-demo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetDemoData(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return Problem(
                title: "Not available",
                detail: "The demo reset endpoint is only enabled in the Development environment.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await DemoDataSeeder.ResetAsync(_db, cancellationToken);
        return NoContent();
    }
}
