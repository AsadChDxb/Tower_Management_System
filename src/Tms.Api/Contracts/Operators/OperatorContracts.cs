using System.ComponentModel.DataAnnotations;

namespace Tms.Api.Contracts.Operators;

public class OperatorDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string CountryCode { get; init; } = string.Empty;

    public string? ContactEmail { get; init; }

    public string? ContactPhone { get; init; }

    public int ActiveTenancies { get; init; }

    public decimal MonthlyRevenue { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}

public class SaveOperatorRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string? ContactEmail { get; set; }

    [MaxLength(40)]
    public string? ContactPhone { get; set; }
}
