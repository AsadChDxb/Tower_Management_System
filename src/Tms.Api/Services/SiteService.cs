using Microsoft.EntityFrameworkCore;
using Tms.Api.Contracts.Common;
using Tms.Api.Contracts.Sites;
using Tms.Domain.Common;
using Tms.Domain.Entities;
using Tms.Domain.Enums;
using Tms.Domain.Rules;
using Tms.Infrastructure.Persistence;

namespace Tms.Api.Services;

public interface ISiteService
{
    Task<PagedResult<SiteListItemDto>> ListAsync(SiteQuery query, CancellationToken cancellationToken);

    Task<SiteDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<SiteDetailDto> CreateAsync(CreateSiteRequest request, CancellationToken cancellationToken);

    Task<SiteDetailDto> UpdateAsync(Guid id, UpdateSiteRequest request, CancellationToken cancellationToken);

    Task<SiteDetailDto> ChangeStatusAsync(Guid id, SiteStatus status, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>Use cases 1 and 3: register/update a site and search the portfolio.</summary>
public class SiteService : ISiteService
{
    private readonly TmsDbContext _db;

    public SiteService(TmsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SiteListItemDto>> ListAsync(SiteQuery query, CancellationToken cancellationToken)
    {
        var sites = _db.Sites.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            sites = sites.Where(s =>
                s.SiteCode.Contains(term) ||
                s.Name.Contains(term) ||
                s.City.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var country = query.Country.Trim().ToUpperInvariant();
            sites = sites.Where(s => s.CountryCode == country);
        }

        if (query.Status.HasValue)
        {
            sites = sites.Where(s => s.Status == query.Status.Value);
        }

        if (query.HasCapacity.HasValue)
        {
            sites = query.HasCapacity.Value
                ? sites.Where(s => s.Towers.Any(t =>
                    t.Tenancies.Count(x => x.Status == TenancyStatus.Active) < t.TenantCapacity))
                : sites.Where(s => s.Towers.Count > 0 && s.Towers.All(t =>
                    t.Tenancies.Count(x => x.Status == TenancyStatus.Active) >= t.TenantCapacity));
        }

        var projected = sites.Select(s => new
        {
            s.Id,
            s.SiteCode,
            s.Name,
            s.CountryCode,
            s.City,
            s.Status,
            s.SiteType,
            s.Latitude,
            s.Longitude,
            s.UpdatedAtUtc,
            TowerCount = s.Towers.Count,
            TenantCapacity = s.Towers.Sum(t => (int?)t.TenantCapacity) ?? 0,
            ActiveTenants = s.Towers.Sum(t => (int?)t.Tenancies.Count(x => x.Status == TenancyStatus.Active)) ?? 0,
            MonthlyRevenue = s.Towers.Sum(t =>
                (decimal?)t.Tenancies
                    .Where(x => x.Status == TenancyStatus.Active)
                    .Sum(x => (decimal?)x.MonthlyRent)) ?? 0m
        });

        var ordered = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "name" => projected.OrderBy(x => x.Name).ThenBy(x => x.SiteCode),
            "country" => projected.OrderBy(x => x.CountryCode).ThenBy(x => x.City),
            "revenue" => projected.OrderByDescending(x => x.MonthlyRevenue).ThenBy(x => x.SiteCode),
            "utilization" => projected
                .OrderByDescending(x => x.TenantCapacity == 0 ? 0d : x.ActiveTenants * 100d / x.TenantCapacity)
                .ThenByDescending(x => x.ActiveTenants),
            _ => projected.OrderBy(x => x.SiteCode)
        };

        var totalCount = await ordered.CountAsync(cancellationToken);
        var rows = await ordered.Skip(query.Skip).Take(query.PageSize).ToListAsync(cancellationToken);

        var items = rows.Select(row =>
        {
            var capacity = new Domain.Portfolio.TowerCapacity(row.TenantCapacity, row.ActiveTenants);

            return new SiteListItemDto
            {
                Id = row.Id,
                SiteCode = row.SiteCode,
                Name = row.Name,
                CountryCode = row.CountryCode,
                City = row.City,
                Status = row.Status,
                SiteType = row.SiteType,
                Latitude = row.Latitude,
                Longitude = row.Longitude,
                TowerCount = row.TowerCount,
                TenantCapacity = row.TenantCapacity,
                ActiveTenants = row.ActiveTenants,
                AvailableSlots = capacity.AvailableSlots,
                UtilizationPercent = capacity.UtilizationPercent,
                MonthlyRevenue = row.MonthlyRevenue,
                UpdatedAtUtc = row.UpdatedAtUtc
            };
        }).ToList();

        return new PagedResult<SiteListItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<SiteDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var site = await LoadSiteAsync(id, asTracking: false, cancellationToken);
        return PortfolioMapping.ToDetailDto(site);
    }

    public async Task<SiteDetailDto> CreateAsync(CreateSiteRequest request, CancellationToken cancellationToken)
    {
        var siteCode = request.SiteCode.Trim().ToUpperInvariant();

        if (await _db.Sites.AnyAsync(s => s.SiteCode == siteCode, cancellationToken))
        {
            throw new DomainRuleException("duplicate_site_code", $"Site code {siteCode} is already registered.");
        }

        var site = new Site
        {
            SiteCode = siteCode,
            Name = request.Name.Trim(),
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SiteType = request.SiteType,
            Status = request.Status,
            AcquiredOn = request.AcquiredOn,
            MonthlySiteCost = request.MonthlySiteCost,
            Notes = request.Notes
        };

        if (request.Tower is not null)
        {
            var towerCode = request.Tower.TowerCode.Trim().ToUpperInvariant();

            if (await _db.Towers.AnyAsync(t => t.TowerCode == towerCode, cancellationToken))
            {
                throw new DomainRuleException("duplicate_tower_code", $"Tower code {towerCode} already exists.");
            }

            site.Towers.Add(new Tower
            {
                TowerCode = towerCode,
                StructureType = request.Tower.StructureType,
                HeightMeters = request.Tower.HeightMeters,
                TenantCapacity = request.Tower.TenantCapacity,
                AntennaMountCapacity = request.Tower.AntennaMountCapacity,
                CommissionedOn = request.Tower.CommissionedOn
            });
        }

        _db.Sites.Add(site);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(site.Id, cancellationToken);
    }

    public async Task<SiteDetailDto> UpdateAsync(Guid id, UpdateSiteRequest request, CancellationToken cancellationToken)
    {
        var site = await LoadSiteAsync(id, asTracking: true, cancellationToken);

        site.Name = request.Name.Trim();
        site.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        site.City = request.City.Trim();
        site.Address = request.Address.Trim();
        site.Latitude = request.Latitude;
        site.Longitude = request.Longitude;
        site.SiteType = request.SiteType;
        site.AcquiredOn = request.AcquiredOn;
        site.MonthlySiteCost = request.MonthlySiteCost;
        site.Notes = request.Notes;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<SiteDetailDto> ChangeStatusAsync(Guid id, SiteStatus status, CancellationToken cancellationToken)
    {
        var site = await LoadSiteAsync(id, asTracking: true, cancellationToken);

        SiteStatusRules.EnsureCanTransition(site.Status, status);

        if (status == SiteStatus.Decommissioned)
        {
            var activeTenancies = site.Towers
                .SelectMany(t => t.Tenancies)
                .Count(t => t.Status == TenancyStatus.Active);

            SiteStatusRules.EnsureNoActiveTenanciesBeforeDecommission(activeTenancies, site.SiteCode);
        }

        site.Status = status;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var site = await LoadSiteAsync(id, asTracking: true, cancellationToken);

        var activeTenancies = site.Towers.SelectMany(t => t.Tenancies).Count(t => t.Status == TenancyStatus.Active);

        if (activeTenancies > 0)
        {
            throw new DomainRuleException(
                "site_has_active_tenancies",
                $"Site {site.SiteCode} still has {activeTenancies} active tenancy(ies) and cannot be deleted.");
        }

        _db.Tenancies.RemoveRange(site.Towers.SelectMany(t => t.Tenancies));
        _db.Towers.RemoveRange(site.Towers);
        _db.Sites.Remove(site);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Site> LoadSiteAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        var query = _db.Sites
            .Include(s => s.Towers)
            .ThenInclude(t => t.Tenancies)
            .ThenInclude(t => t.Operator)
            .AsQueryable();

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
               ?? throw new KeyNotFoundException($"Site {id} was not found.");
    }
}
