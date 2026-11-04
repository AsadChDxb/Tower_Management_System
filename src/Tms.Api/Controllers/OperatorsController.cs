using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tms.Api.Auth;
using Tms.Api.Contracts.Operators;
using Tms.Api.Services;

namespace Tms.Api.Controllers;

/// <summary>Operator (tenant company) directory used by the commercial team.</summary>
[ApiController]
[Route("api/operators")]
[Authorize]
[Tags("Operators")]
public class OperatorsController : ControllerBase
{
    private readonly IOperatorService _operators;

    public OperatorsController(IOperatorService operators)
    {
        _operators = operators;
    }

    /// <summary>All operators with their active tenancy count and monthly revenue.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<IReadOnlyList<OperatorDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OperatorDto>>> List([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _operators.ListAsync(search, cancellationToken));

    /// <summary>Single operator.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PortfolioView)]
    [ProducesResponseType<OperatorDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OperatorDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _operators.GetAsync(id, cancellationToken));

    /// <summary>Adds a new operator to the directory.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.OperatorsManage)]
    [ProducesResponseType<OperatorDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OperatorDto>> Create([FromBody] SaveOperatorRequest request, CancellationToken cancellationToken)
    {
        var created = await _operators.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Updates operator contact details.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.OperatorsManage)]
    [ProducesResponseType<OperatorDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OperatorDto>> Update(Guid id, [FromBody] SaveOperatorRequest request, CancellationToken cancellationToken) =>
        Ok(await _operators.UpdateAsync(id, request, cancellationToken));
}
