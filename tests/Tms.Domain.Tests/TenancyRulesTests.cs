using Tms.Domain.Common;
using Tms.Domain.Enums;
using Tms.Domain.Portfolio;
using Tms.Domain.Rules;

namespace Tms.Domain.Tests;

public class TenancyRulesTests
{
    [Fact]
    public void A_tower_with_a_free_slot_accepts_the_assignment()
    {
        TenancyRules.EnsureCapacityAvailable(new TowerCapacity(4, 3), "DE-BER-0142-T1");
    }

    [Fact]
    public void A_full_tower_rejects_the_assignment_as_a_conflict()
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            TenancyRules.EnsureCapacityAvailable(new TowerCapacity(4, 4), "DE-HAM-0087-T1"));

        Assert.Equal("tower_capacity_exceeded", exception.RuleCode);
        Assert.True(exception.IsConflict);
        Assert.Contains("DE-HAM-0087-T1", exception.Message);
    }

    [Fact]
    public void An_operator_cannot_hold_two_active_tenancies_on_the_same_tower()
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            TenancyRules.EnsureNoDuplicateActiveTenancy(true, "NL-AMS-0011-T2", "Batavia Mobile B.V."));

        Assert.Equal("duplicate_active_tenancy", exception.RuleCode);
        Assert.Contains("Batavia Mobile B.V.", exception.Message);
    }

    [Theory]
    [InlineData(SiteStatus.Planned)]
    [InlineData(SiteStatus.Decommissioned)]
    public void Only_active_sites_can_be_leased(SiteStatus status)
    {
        var exception = Assert.Throws<DomainRuleException>(() =>
            TenancyRules.EnsureSiteIsLeasable(status, "FR-TLS-0044"));

        Assert.Equal("site_not_leasable", exception.RuleCode);
        Assert.Equal(SiteStatus.Active, SiteStatus.Active);
    }

    [Fact]
    public void Active_sites_are_leasable() =>
        TenancyRules.EnsureSiteIsLeasable(SiteStatus.Active, "DE-BER-0142");

    [Fact]
    public void An_end_date_before_the_start_date_is_a_bad_request_not_a_conflict()
    {
        var start = new DateOnly(2026, 1, 10);
        var end = new DateOnly(2026, 1, 9);

        var exception = Assert.Throws<DomainRuleException>(() => TenancyRules.EnsureValidTerm(start, end));

        Assert.Equal("invalid_tenancy_term", exception.RuleCode);
        Assert.False(exception.IsConflict);
    }

    [Fact]
    public void An_open_ended_term_is_valid() =>
        TenancyRules.EnsureValidTerm(new DateOnly(2026, 1, 10), null);

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Rent_must_be_positive(decimal rent)
    {
        var exception = Assert.Throws<DomainRuleException>(() => TenancyRules.EnsureValidRent(rent));

        Assert.Equal("invalid_monthly_rent", exception.RuleCode);
        Assert.False(exception.IsConflict);
    }

    [Fact]
    public void Only_active_tenancies_can_be_terminated()
    {
        TenancyRules.EnsureTerminable(TenancyStatus.Active);

        var exception = Assert.Throws<DomainRuleException>(() =>
            TenancyRules.EnsureTerminable(TenancyStatus.Terminated));

        Assert.Equal("tenancy_not_active", exception.RuleCode);
    }
}
