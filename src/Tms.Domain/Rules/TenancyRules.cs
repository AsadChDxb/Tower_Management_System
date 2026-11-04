using Tms.Domain.Common;
using Tms.Domain.Enums;
using Tms.Domain.Portfolio;

namespace Tms.Domain.Rules;

/// <summary>
/// Leasing rules: capacity, duplicate tenancies, site readiness, dates and rent.
/// Kept in the domain so the API, the seeder and the tests all enforce the same thing.
/// </summary>
public static class TenancyRules
{
    public static TowerCapacity Capacity(int tenantCapacity, int activeTenants) =>
        new(tenantCapacity, activeTenants);

    /// <summary>Use case 2: a tower can only take another tenant if a slot is free.</summary>
    public static void EnsureCapacityAvailable(TowerCapacity capacity, string towerCode)
    {
        if (capacity.CanAcceptTenant)
        {
            return;
        }

        throw new DomainRuleException(
            "tower_capacity_exceeded",
            $"Tower {towerCode} is fully utilized ({capacity.ActiveTenants}/{capacity.TenantCapacity} tenant slots in use). " +
            "Terminate an existing tenancy or increase the tower capacity before assigning another operator.");
    }

    public static void EnsureNoDuplicateActiveTenancy(bool operatorAlreadyLeasesTower, string towerCode, string operatorName)
    {
        if (!operatorAlreadyLeasesTower)
        {
            return;
        }

        throw new DomainRuleException(
            "duplicate_active_tenancy",
            $"{operatorName} already has an active tenancy on tower {towerCode}.");
    }

    /// <summary>Only live sites can be leased; planned and decommissioned sites cannot.</summary>
    public static void EnsureSiteIsLeasable(SiteStatus siteStatus, string siteCode)
    {
        if (siteStatus == SiteStatus.Active)
        {
            return;
        }

        throw new DomainRuleException(
            "site_not_leasable",
            $"Site {siteCode} is {siteStatus}. Tenancies can only be created on Active sites.");
    }

    public static void EnsureValidTerm(DateOnly startDate, DateOnly? endDate)
    {
        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new DomainRuleException(
                "invalid_tenancy_term",
                "Tenancy end date must be on or after the start date.",
                isConflict: false);
        }
    }

    public static void EnsureValidRent(decimal monthlyRent)
    {
        if (monthlyRent <= 0)
        {
            throw new DomainRuleException(
                "invalid_monthly_rent",
                "Monthly rent must be greater than zero.",
                isConflict: false);
        }
    }

    public static void EnsureTerminable(TenancyStatus status)
    {
        if (status == TenancyStatus.Active)
        {
            return;
        }

        throw new DomainRuleException(
            "tenancy_not_active",
            $"Only active tenancies can be terminated (current status: {status}).");
    }
}
