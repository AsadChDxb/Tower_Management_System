using Tms.Domain.Portfolio;

namespace Tms.Domain.Tests;

public class TowerCapacityTests
{
    [Theory]
    [InlineData(4, 3, 1, true, 75.0)]
    [InlineData(3, 3, 0, false, 100.0)]
    [InlineData(2, 0, 2, true, 0.0)]
    [InlineData(5, 1, 4, true, 20.0)]
    public void Reports_slots_availability_and_utilization(
        int tenantCapacity,
        int activeTenants,
        int expectedAvailable,
        bool expectedCanAccept,
        double expectedUtilization)
    {
        var capacity = new TowerCapacity(tenantCapacity, activeTenants);

        Assert.Equal(expectedAvailable, capacity.AvailableSlots);
        Assert.Equal(expectedCanAccept, capacity.CanAcceptTenant);
        Assert.Equal(expectedUtilization, capacity.UtilizationPercent, 1);
    }

    [Fact]
    public void Over_committed_tower_never_reports_negative_slots()
    {
        var capacity = new TowerCapacity(2, 3);

        Assert.Equal(0, capacity.AvailableSlots);
        Assert.False(capacity.CanAcceptTenant);
        Assert.Equal(150.0, capacity.UtilizationPercent, 1);
    }

    [Fact]
    public void Zero_capacity_is_reported_as_zero_percent() =>
        Assert.Equal(0, new TowerCapacity(0, 0).UtilizationPercent);
}
