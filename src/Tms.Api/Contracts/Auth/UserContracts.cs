using System.ComponentModel.DataAnnotations;

namespace Tms.Api.Contracts.Auth;

public class CreateUserRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<string> Roles { get; set; } = [];
}

public class UpdateUserRequest
{
    [MaxLength(120)]
    public string? DisplayName { get; set; }

    public bool? IsActive { get; set; }

    public List<string>? Roles { get; set; }
}

public class ResetPasswordRequest
{
    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}
