using Microsoft.AspNetCore.Identity;
using Tms.Api.Auth;
using Tms.Api.Contracts.Auth;
using Tms.Domain.Common;
using Tms.Infrastructure.Identity;

namespace Tms.Api.Services;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, Guid actingUserId, CancellationToken cancellationToken);

    Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken);
}

/// <summary>Admin-only user administration: create users, assign roles, deactivate, reset password.</summary>
public class UserAdminService : IUserAdminService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UserAdminService(UserManager<AppUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        var users = _userManager.Users.OrderBy(u => u.DisplayName).ToList();
        var result = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(ToDto(user, roles));
        }

        return result;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        await EnsureRolesExistAsync(request.Roles);

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            throw new DomainRuleException("duplicate_user_email", $"A user with e-mail {email} already exists.");
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim(),
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        EnsureSucceeded(createResult);

        var roleResult = await _userManager.AddToRolesAsync(user, request.Roles.Distinct());
        EnsureSucceeded(roleResult);

        return ToDto(user, request.Roles.Distinct().ToList());
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, Guid actingUserId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
                   ?? throw new KeyNotFoundException($"User {id} was not found.");

        if (id == actingUserId && request.IsActive == false)
        {
            throw new DomainRuleException("cannot_deactivate_self", "You cannot deactivate your own account.");
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            user.DisplayName = request.DisplayName.Trim();
            EnsureSucceeded(await _userManager.UpdateAsync(user));
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
            EnsureSucceeded(await _userManager.UpdateAsync(user));
        }

        if (request.Roles is not null)
        {
            await EnsureRolesExistAsync(request.Roles);

            if (id == actingUserId && !request.Roles.Contains(AppRoles.Admin))
            {
                throw new DomainRuleException("cannot_remove_own_admin_role", "You cannot remove your own Admin role.");
            }

            var current = await _userManager.GetRolesAsync(user);
            var toAdd = request.Roles.Except(current).ToList();
            var toRemove = current.Except(request.Roles).ToList();

            if (toAdd.Count > 0)
            {
                EnsureSucceeded(await _userManager.AddToRolesAsync(user, toAdd));
            }

            if (toRemove.Count > 0)
            {
                EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, toRemove));
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        return ToDto(user, roles);
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
                   ?? throw new KeyNotFoundException($"User {id} was not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        EnsureSucceeded(await _userManager.ResetPasswordAsync(user, token, newPassword));
    }

    private async Task EnsureRolesExistAsync(IEnumerable<string> roles)
    {
        foreach (var role in roles.Distinct())
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                throw new DomainRuleException(
                    "unknown_role",
                    $"Role '{role}' does not exist. Valid roles: {string.Join(", ", AppRoles.All)}.",
                    isConflict: false);
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var message = string.Join(" ", result.Errors.Select(e => e.Description));
        throw new DomainRuleException("identity_operation_failed", message, isConflict: false);
    }

    private static UserDto ToDto(AppUser user, IEnumerable<string> roles)
    {
        var roleList = roles.ToList();

        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.IsActive,
            roleList,
            Permissions.ForRoles(roleList),
            user.CreatedAtUtc);
    }
}
