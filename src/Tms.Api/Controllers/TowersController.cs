using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tms.Api.Auth;
using Tms.Api.Contracts.Sites;
using Tms.Api.Services;

namespace Tms.Api.Controllers;

/// <summary>Tower level operations, including the capacity question of use case 2.</summary>
[ApiController]
[Route("api/towers")]
[Authorize]
[Tags("Towers")]
public class TowersController : ControllerBase
{
    private readonly ITowerService _towers;

    public TowersController(ITowerService towers)
    {
        _towers = towers;
    }

    /// <summary>One tower with its tenant list and capacity.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<TowerDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TowerDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _towers.GetAsync(id, cancellationToken));

    /// <summary>Updates structural and capacity attributes of a tower.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.TowersManage)]
    [ProducesResponseType<TowerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TowerDto>> Update(Guid id, [FromBody] UpdateTowerRequest request, CancellationToken cancellationToken) =>
        Ok(await _towers.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// "Can this tower take another tenant?" - returns slots, utilization and a readable summary.
    /// </summary>
    [HttpGet("{id:guid}/capacity")]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<TowerCapacityDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TowerCapacityDto>> GetCapacity(Guid id, CancellationToken cancellationToken) =>
        Ok(await _towers.GetCapacityAsync(id, cancellationToken));
}
