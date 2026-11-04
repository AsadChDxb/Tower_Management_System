using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tms.Api.Auth;
using Tms.Api.Contracts.Common;
using Tms.Api.Contracts.Sites;
using Tms.Api.Contracts.Tenancies;
using Tms.Api.Services;

namespace Tms.Api.Controllers;

/// <summary>Use case 2: assign operators to towers, adjust and terminate tenancies.</summary>
[ApiController]
[Route("api/tenancies")]
[Authorize]
[Tags("Tenancies")]
public class TenanciesController : ControllerBase
{
    private readonly ITenancyService _tenancies;

    public TenanciesController(ITenancyService tenancies)
    {
        _tenancies = tenancies;
    }

    /// <summary>Paged tenancy list, filterable by tower, operator, status, country and free text.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<PagedResult<TenancyDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TenancyDto>>> List([FromQuery] TenancyQuery query, CancellationToken cancellationToken) =>
        Ok(await _tenancies.ListAsync(query, cancellationToken));

    /// <summary>Single tenancy.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<TenancyDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TenancyDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _tenancies.GetAsync(id, cancellationToken));

    /// <summary>
    /// Assigns an operator to a tower. Rejects the request with 409 when the tower is full,
    /// the operator already leases that tower, or the site is not active.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.TenanciesManage)]
    [ProducesResponseType<TenancyDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TenancyDto>> Create([FromBody] CreateTenancyRequest request, CancellationToken cancellationToken)
    {
        var created = await _tenancies.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Updates commercial terms of an existing tenancy.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.TenanciesManage)]
    [ProducesResponseType<TenancyDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TenancyDto>> Update(Guid id, [FromBody] UpdateTenancyRequest request, CancellationToken cancellationToken) =>
        Ok(await _tenancies.UpdateAsync(id, request, cancellationToken));

    /// <summary>Terminates a tenancy, freeing the tenant slot on the tower immediately.</summary>
    [HttpPost("{id:guid}/terminate")]
    [Authorize(Policy = Permissions.TenanciesManage)]
    [ProducesResponseType<TenancyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TenancyDto>> Terminate(Guid id, [FromBody] TerminateTenancyRequest request, CancellationToken cancellationToken) =>
        Ok(await _tenancies.TerminateAsync(id, request, cancellationToken));
}
