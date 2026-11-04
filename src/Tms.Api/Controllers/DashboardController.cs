using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tms.Api.Auth;
using Tms.Api.Contracts.Dashboard;
using Tms.Api.Contracts.Lookups;
using Tms.Api.Services;

namespace Tms.Api.Controllers;

/// <summary>Use case 4: the manager's portfolio overview (map + charts + KPIs).</summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Permissions.DashboardView)]
[Tags("Dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>KPIs, country split, status split, utilization buckets, top operators, map pins.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken cancellationToken) =>
        Ok(await _dashboard.GetSummaryAsync(cancellationToken));
}

/// <summary>Reference data for forms and filters.</summary>
[ApiController]
[Route("api/lookups")]
[Authorize]
[Tags("Lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookups;

    public LookupsController(ILookupService lookups)
    {
        _lookups = lookups;
    }

    /// <summary>Countries, enums, currencies, roles, permissions and allowed status transitions.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<LookupsDto>(StatusCodes.Status200OK)]
    public ActionResult<LookupsDto> Get() => Ok(_lookups.Get());
}
