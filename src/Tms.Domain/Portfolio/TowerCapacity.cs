namespace Tms.Domain.Portfolio;

/// <summary>
/// Derived capacity view of a tower. Never persisted - active tenancies are the single
/// source of truth, so a counter can never drift out of sync with the data.
/// </summary>
public readonly record struct TowerCapacity(int TenantCapacity, int ActiveTenants)
{
    public int AvailableSlots => Math.Max(0, TenantCapacity - ActiveTenants);

    public bool CanAcceptTenant => AvailableSlots > 0;

    public double UtilizationPercent =>
        TenantCapacity <= 0 ? 0 : Math.Round(ActiveTenants * 100d / TenantCapacity, 1);
}
