using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tms.Api.Auth;
using Tms.Api.Contracts.Auth;
using Tms.Api.Services;

namespace Tms.Api.Controllers;

/// <summary>Admin-only user administration: create users, assign roles, activate/deactivate.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.UsersManage)]
[Tags("Users")]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _users;

    public UsersController(IUserAdminService users)
    {
        _users = users;
    }

    /// <summary>Lists every user with roles, permissions and active flag.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken cancellationToken) =>
        Ok(await _users.ListAsync(cancellationToken));

    /// <summary>Creates a user and assigns one or more roles.</summary>
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _users.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    /// <summary>Updates display name, active flag and/or roles.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken) =>
        Ok(await _users.UpdateAsync(id, request, User.GetUserId(), cancellationToken));

    /// <summary>Admin password reset (no e-mail loop in this demo).</summary>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _users.ResetPasswordAsync(id, request.NewPassword, cancellationToken);
        return NoContent();
    }
}
