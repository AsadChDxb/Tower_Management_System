using Tms.Domain.Common;
using Tms.Domain.Enums;
using Tms.Domain.Rules;

namespace Tms.Domain.Tests;

public class SiteStatusRulesTests
{
    [Theory]
    [InlineData(SiteStatus.Planned, SiteStatus.Active, true)]
    [InlineData(SiteStatus.Planned, SiteStatus.Decommissioned, true)]
    [InlineData(SiteStatus.Planned, SiteStatus.Planned, true)]
    [InlineData(SiteStatus.Active, SiteStatus.Decommissioned, true)]
    [InlineData(SiteStatus.Active, SiteStatus.Planned, false)]
    [InlineData(SiteStatus.Decommissioned, SiteStatus.Active, false)]
    [InlineData(SiteStatus.Decommissioned, SiteStatus.Planned, false)]
    public void Lifecycle_transitions_follow_the_state_machine(
        SiteStatus from,
        SiteStatus to,
        bool expected) =>
        Assert.Equal(expected, SiteStatusRules.CanTransition(from, to));

    [Fact]
    public void Decommissioned_is_terminal()
    {
        Assert.Empty(SiteStatusRules.AllowedNextStates(SiteStatus.Decommissioned));
    }

    [Fact]
    public void Going_backwards_from_active_to_planned_is_rejected_with_a_conflict()
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            SiteStatusRules.EnsureCanTransition(SiteStatus.Active, SiteStatus.Planned));

        Assert.Equal("invalid_site_status_transition", exception.RuleCode);
        Assert.True(exception.IsConflict);
        Assert.Contains("Decommissioned", exception.Message);
    }

    [Fact]
    public void A_site_with_active_tenancies_cannot_be_decommissioned()
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            SiteStatusRules.EnsureNoActiveTenanciesBeforeDecommission(2, "NL-AMS-0011"));

        Assert.Equal("site_has_active_tenancies", exception.RuleCode);
        Assert.Contains("2 active tenancy", exception.Message);
    }

    [Fact]
    public void A_site_without_tenants_can_be_decommissioned() =>
        SiteStatusRules.EnsureNoActiveTenanciesBeforeDecommission(0, "NL-EIN-0027");
}
