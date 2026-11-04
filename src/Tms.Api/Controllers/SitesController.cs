using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tms.Api.Auth;
using Tms.Api.Contracts.Common;
using Tms.Api.Contracts.Sites;
using Tms.Api.Services;

namespace Tms.Api.Controllers;

/// <summary>Use cases 1 and 3: register/update sites, search the portfolio, open one site.</summary>
[ApiController]
[Route("api/sites")]
[Authorize]
[Tags("Sites")]
public class SitesController : ControllerBase
{
    private readonly ISiteService _sites;
    private readonly ITowerService _towers;

    public SitesController(ISiteService sites, ITowerService towers)
    {
        _sites = sites;
        _towers = towers;
    }

    /// <summary>Paged site search with free-text, country, status and capacity filters.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<PagedResult<SiteListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SiteListItemDto>>> List([FromQuery] SiteQuery query, CancellationToken cancellationToken) =>
        Ok(await _sites.ListAsync(query, cancellationToken));

    /// <summary>Everything about one site: facts, towers, tenants, capacity and revenue.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<SiteDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDetailDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sites.GetAsync(id, cancellationToken));

    /// <summary>Registers a new site, optionally with its first tower.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.SitesManage)]
    [ProducesResponseType<SiteDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SiteDetailDto>> Create([FromBody] CreateSiteRequest request, CancellationToken cancellationToken)
    {
        var created = await _sites.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Updates the mutable site details (site code is the immutable business key).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.SitesManage)]
    [ProducesResponseType<SiteDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SiteDetailDto>> Update(Guid id, [FromBody] UpdateSiteRequest request, CancellationToken cancellationToken) =>
        Ok(await _sites.UpdateAsync(id, request, cancellationToken));

    /// <summary>Moves a site through its lifecycle (Planned -> Active -> Decommissioned).</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.SitesManage)]
    [ProducesResponseType<SiteDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SiteDetailDto>> ChangeStatus(Guid id, [FromBody] ChangeSiteStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await _sites.ChangeStatusAsync(id, request.Status!.Value, cancellationToken));

    /// <summary>Deletes a site and its towers - blocked while active tenancies exist.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.SitesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sites.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Towers on a site with their live capacity.</summary>
    [HttpGet("{siteId:guid}/towers")]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<IReadOnlyList<TowerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TowerDto>>> ListTowers(Guid siteId, CancellationToken cancellationToken) =>
        Ok(await _towers.ListBySiteAsync(siteId, cancellationToken));

    /// <summary>Adds another tower to an existing site.</summary>
    [HttpPost("{siteId:guid}/towers")]
    [Authorize(Policy = Permissions.TowersManage)]
    [ProducesResponseType<TowerDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TowerDto>> CreateTower(Guid siteId, [FromBody] CreateTowerRequest request, CancellationToken cancellationToken)
    {
        var created = await _towers.CreateAsync(siteId, request, cancellationToken);
        return CreatedAtAction("Get", "Towers", new { id = created.Id }, created);
    }
}
