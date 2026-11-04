namespace Tms.Domain.Common;

/// <summary>
/// Raised when a business rule is violated. The API maps it to an RFC 7807 problem response:
/// <see cref="IsConflict"/> = true becomes 409 Conflicted, otherwise 400 Bad Request.
/// The domain stays HTTP-agnostic; only this flag hints at the right status code.
/// </summary>
public sealed class DomainRuleException : Exception
{
    public DomainRuleException(string ruleCode, string message, bool isConflict = true)
        : base(message)
    {
        RuleCode = ruleCode;
        IsConflict = isConflict;
    }

    /// <summary>Stable machine-readable code, e.g. <c>tower_capacity_exceeded</c>.</summary>
    public string RuleCode { get; }

    public bool IsConflict { get; }
}
