using Microsoft.EntityFrameworkCore;
using Tms.Api.Contracts.Dashboard;
using Tms.Domain.Enums;
using Tms.Domain.Portfolio;
using Tms.Infrastructure.Persistence;

namespace Tms.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Use case 4: portfolio overview. Aggregates are computed from a single tower snapshot so
/// every number on the dashboard (KPIs, country chart, map, colour buckets) agrees.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly TmsDbContext _db;

    public DashboardService(TmsDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var towers = await _db.Towers
            .AsNoTracking()
            .Select(t => new TowerSnapshot
            {
                TowerId = t.Id,
                TowerCode = t.TowerCode,
                SiteId = t.SiteId,
                SiteCode = t.Site!.SiteCode,
                SiteName = t.Site!.Name,
                CountryCode = t.Site!.CountryCode,
                City = t.Site!.City,
                SiteStatus = t.Site!.Status,
                TenantCapacity = t.TenantCapacity,
                ActiveTenants = t.Tenancies.Count(x => x.Status == TenancyStatus.Active),
                MonthlyRevenue = t.Tenancies
                    .Where(x => x.Status == TenancyStatus.Active)
                    .Sum(x => (decimal?)x.MonthlyRent) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var sites = await _db.Sites
            .AsNoTracking()
            .Select(s => new SiteSnapshot
            {
                Id = s.Id,
                SiteCode = s.SiteCode,
                Name = s.Name,
                CountryCode = s.CountryCode,
                City = s.City,
                Status = s.Status,
                Latitude = s.Latitude,
                Longitude = s.Longitude
            })
            .ToListAsync(cancellationToken);

        var operatorRevenue = await _db.Tenancies
            .AsNoTracking()
            .Where(t => t.Status == TenancyStatus.Active)
            .GroupBy(t => new { t.OperatorId, t.Operator!.Name, t.Operator!.CountryCode })
            .Select(g => new OperatorRevenueRow
            {
                OperatorId = g.Key.OperatorId,
                Name = g.Key.Name,
                CountryCode = g.Key.CountryCode,
                ActiveTenancies = g.Count(),
                MonthlyRevenue = g.Sum(t => t.MonthlyRent)
            })
            .OrderByDescending(x => x.MonthlyRevenue)
            .ToListAsync(cancellationToken);

        var totalCapacity = towers.Sum(t => t.TenantCapacity);
        var totalActive = towers.Sum(t => t.ActiveTenants);
        var totalRevenue = towers.Sum(t => t.MonthlyRevenue);
        var portfolioCapacity = new TowerCapacity(totalCapacity, totalActive);

        var kpis = new DashboardKpiDto
        {
            SiteCount = sites.Count,
            ActiveSiteCount = sites.Count(s => s.Status == SiteStatus.Active),
            PlannedSiteCount = sites.Count(s => s.Status == SiteStatus.Planned),
            DecommissionedSiteCount = sites.Count(s => s.Status == SiteStatus.Decommissioned),
            TowerCount = towers.Count,
            TenantCapacity = totalCapacity,
            ActiveTenants = totalActive,
            AvailableSlots = portfolioCapacity.AvailableSlots,
            FullyUtilizedTowerCount = towers.Count(t =>
                new TowerCapacity(t.TenantCapacity, t.ActiveTenants).AvailableSlots == 0),
            UtilizationPercent = portfolioCapacity.UtilizationPercent,
            MonthlyRevenue = totalRevenue,
            AverageRevenuePerTenant = totalActive == 0 ? 0m : Math.Round(totalRevenue / totalActive, 2)
        };

        return new DashboardSummaryDto
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Kpis = kpis,
            ByCountry = BuildCountrySummaries(towers, sites),
            ByStatus = sites
                .GroupBy(s => s.Status)
                .OrderBy(g => g.Key)
                .Select(g => new StatusBreakdownDto { Status = g.Key.ToString(), SiteCount = g.Count() })
                .ToList(),
            UtilizationBuckets = BuildUtilizationBuckets(towers),
            TopOperatorsByRevenue = BuildTopOperators(operatorRevenue, totalRevenue),
            UnderUtilizedTowers = BuildUnderUtilized(towers),
            MapSites = BuildMapSites(sites, towers)
        };
    }

    private static List<CountrySummaryDto> BuildCountrySummaries(
        IReadOnlyList<TowerSnapshot> towers,
        IReadOnlyList<SiteSnapshot> sites) =>
        towers
            .GroupBy(t => t.CountryCode)
            .Select(group =>
            {
                var capacity = group.Sum(t => t.TenantCapacity);
                var active = group.Sum(t => t.ActiveTenants);
                var snapshot = new TowerCapacity(capacity, active);

                return new CountrySummaryDto
                {
                    CountryCode = group.Key,
                    SiteCount = sites.Count(s => s.CountryCode == group.Key),
                    TowerCount = group.Count(),
                    TenantCapacity = capacity,
                    ActiveTenants = active,
                    AvailableSlots = snapshot.AvailableSlots,
                    UtilizationPercent = snapshot.UtilizationPercent,
                    MonthlyRevenue = group.Sum(t => t.MonthlyRevenue)
                };
            })
            .OrderByDescending(c => c.MonthlyRevenue)
            .ToList();

    private static List<UtilizationBucketDto> BuildUtilizationBuckets(IReadOnlyList<TowerSnapshot> towers)
    {
        var buckets = new (string Label, Func<TowerCapacity, bool> Matches)[]
        {
            ("Idle (0%)", c => c.ActiveTenants == 0),
            ("Low (1-49%)", c => c.UtilizationPercent is > 0 and < 50),
            ("Healthy (50-99%)", c => c.UtilizationPercent is >= 50 and < 100),
            ("Full (100%)", c => c.UtilizationPercent >= 100)
        };

        return buckets
            .Select(b => new UtilizationBucketDto
            {
                Label = b.Label,
                TowerCount = towers.Count(t => b.Matches(new TowerCapacity(t.TenantCapacity, t.ActiveTenants)))
            })
            .ToList();
    }

    private static List<OperatorRevenueDto> BuildTopOperators(
        IEnumerable<OperatorRevenueRow> rows,
        decimal totalRevenue) =>
        rows
            .Take(5)
            .Select(o => new OperatorRevenueDto
            {
                OperatorId = o.OperatorId,
                Name = o.Name,
                CountryCode = o.CountryCode,
                ActiveTenancies = o.ActiveTenancies,
                MonthlyRevenue = o.MonthlyRevenue,
                RevenueSharePercent = totalRevenue == 0
                    ? 0
                    : Math.Round((double)(o.MonthlyRevenue / totalRevenue) * 100, 1)
            })
            .ToList();

    private static List<UnderUtilizedTowerDto> BuildUnderUtilized(IReadOnlyList<TowerSnapshot> towers) =>
        towers
            .Select(t => (Tower: t, Capacity: new TowerCapacity(t.TenantCapacity, t.ActiveTenants)))
            .Where(x => x.Capacity.AvailableSlots > 0 && x.Tower.SiteStatus == SiteStatus.Active)
            .OrderByDescending(x => x.Capacity.AvailableSlots)
            .ThenBy(x => x.Capacity.UtilizationPercent)
            .Take(6)
            .Select(x => new UnderUtilizedTowerDto
            {
                TowerId = x.Tower.TowerId,
                TowerCode = x.Tower.TowerCode,
                SiteId = x.Tower.SiteId,
                SiteCode = x.Tower.SiteCode,
                SiteName = x.Tower.SiteName,
                CountryCode = x.Tower.CountryCode,
                City = x.Tower.City,
                TenantCapacity = x.Capacity.TenantCapacity,
                ActiveTenants = x.Capacity.ActiveTenants,
                AvailableSlots = x.Capacity.AvailableSlots,
                UtilizationPercent = x.Capacity.UtilizationPercent
            })
            .ToList();

    private static List<MapSiteDto> BuildMapSites(
        IReadOnlyList<SiteSnapshot> sites,
        IReadOnlyList<TowerSnapshot> towers) =>
        sites
            .Select(site =>
            {
                var siteTowers = towers.Where(t => t.SiteId == site.Id).ToList();
                var capacity = new TowerCapacity(
                    siteTowers.Sum(t => t.TenantCapacity),
                    siteTowers.Sum(t => t.ActiveTenants));

                return new MapSiteDto
                {
                    Id = site.Id,
                    SiteCode = site.SiteCode,
                    Name = site.Name,
                    CountryCode = site.CountryCode,
                    City = site.City,
                    Status = site.Status.ToString(),
                    Latitude = site.Latitude,
                    Longitude = site.Longitude,
                    TowerCount = siteTowers.Count,
                    TenantCapacity = capacity.TenantCapacity,
                    ActiveTenants = capacity.ActiveTenants,
                    UtilizationPercent = capacity.UtilizationPercent
                };
            })
            .OrderByDescending(s => s.UtilizationPercent)
            .ToList();

    private sealed class TowerSnapshot
    {
        public Guid TowerId { get; init; }

        public string TowerCode { get; init; } = string.Empty;

        public Guid SiteId { get; init; }

        public string SiteCode { get; init; } = string.Empty;

        public string SiteName { get; init; } = string.Empty;

        public string CountryCode { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public SiteStatus SiteStatus { get; init; }

        public int TenantCapacity { get; init; }

        public int ActiveTenants { get; init; }

        public decimal MonthlyRevenue { get; init; }
    }

    private sealed class SiteSnapshot
    {
        public Guid Id { get; init; }

        public string SiteCode { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string CountryCode { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public SiteStatus Status { get; init; }

        public double Latitude { get; init; }

        public double Longitude { get; init; }
    }

    private sealed class OperatorRevenueRow
    {
        public Guid OperatorId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string CountryCode { get; init; } = string.Empty;

        public int ActiveTenancies { get; init; }

        public decimal MonthlyRevenue { get; init; }
    }
}
