using Tms.Domain.Common;
using Tms.Domain.Enums;

namespace Tms.Domain.Rules;

/// <summary>
/// Site lifecycle: Planned -> Active -> Decommissioned. Decommissioned is terminal.
/// </summary>
public static class SiteStatusRules
{
    private static readonly IReadOnlyDictionary<SiteStatus, SiteStatus[]> Allowed =
        new Dictionary<SiteStatus, SiteStatus[]>
        {
            [SiteStatus.Planned] = [SiteStatus.Active, SiteStatus.Decommissioned],
            [SiteStatus.Active] = [SiteStatus.Decommissioned],
            [SiteStatus.Decommissioned] = []
        };

    public static bool CanTransition(SiteStatus from, SiteStatus to) =>
        from == to || Allowed[from].Contains(to);

    public static IReadOnlyList<SiteStatus> AllowedNextStates(SiteStatus from) => Allowed[from];

    public static void EnsureCanTransition(SiteStatus from, SiteStatus to)
    {
        if (CanTransition(from, to))
        {
            return;
        }

        throw new DomainRuleException(
            "invalid_site_status_transition",
            $"Site status cannot move from {from} to {to}. Allowed next states: " +
            $"{(Allowed[from].Length == 0 ? "none (terminal state)" : string.Join(", ", Allowed[from]))}.");
    }

    /// <summary>We cannot retire a site while operators are still paying for it.</summary>
    public static void EnsureNoActiveTenanciesBeforeDecommission(int activeTenancies, string siteCode)
    {
        if (activeTenancies == 0)
        {
            return;
        }

        throw new DomainRuleException(
            "site_has_active_tenancies",
            $"Site {siteCode} still has {activeTenancies} active tenancy(ies). Terminate them before decommissioning the site.");
    }
}
