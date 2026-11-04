using Microsoft.EntityFrameworkCore;
using Tms.Api.Contracts.Sites;
using Tms.Domain.Common;
using Tms.Domain.Entities;
using Tms.Domain.Enums;
using Tms.Infrastructure.Persistence;

namespace Tms.Api.Services;

public interface ITowerService
{
    Task<IReadOnlyList<TowerDto>> ListBySiteAsync(Guid siteId, CancellationToken cancellationToken);

    Task<TowerDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<TowerCapacityDto> GetCapacityAsync(Guid id, CancellationToken cancellationToken);

    Task<TowerDto> CreateAsync(Guid siteId, CreateTowerRequest request, CancellationToken cancellationToken);

    Task<TowerDto> UpdateAsync(Guid id, UpdateTowerRequest request, CancellationToken cancellationToken);
}

public class TowerService : ITowerService
{
    private readonly TmsDbContext _db;

    public TowerService(TmsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TowerDto>> ListBySiteAsync(Guid siteId, CancellationToken cancellationToken)
    {
        if (!await _db.Sites.AnyAsync(s => s.Id == siteId, cancellationToken))
        {
            throw new KeyNotFoundException($"Site {siteId} was not found.");
        }

        var towers = await LoadTowers()
            .AsNoTracking()
            .Where(t => t.SiteId == siteId)
            .OrderBy(t => t.TowerCode)
            .ToListAsync(cancellationToken);

        return towers.Select(PortfolioMapping.ToTowerDto).ToList();
    }

    public async Task<TowerDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var tower = await LoadTowers().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
                    ?? throw new KeyNotFoundException($"Tower {id} was not found.");

        return PortfolioMapping.ToTowerDto(tower);
    }

    public async Task<TowerCapacityDto> GetCapacityAsync(Guid id, CancellationToken cancellationToken)
    {
        var tower = await LoadTowers().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
                    ?? throw new KeyNotFoundException($"Tower {id} was not found.");

        var activeTenants = tower.Tenancies.Count(t => t.Status == TenancyStatus.Active);

        return PortfolioMapping.ToCapacityDto(tower, activeTenants);
    }

    public async Task<TowerDto> CreateAsync(Guid siteId, CreateTowerRequest request, CancellationToken cancellationToken)
    {
        if (!await _db.Sites.AnyAsync(s => s.Id == siteId, cancellationToken))
        {
            throw new KeyNotFoundException($"Site {siteId} was not found.");
        }

        var towerCode = request.TowerCode.Trim().ToUpperInvariant();

        if (await _db.Towers.AnyAsync(t => t.TowerCode == towerCode, cancellationToken))
        {
            throw new DomainRuleException("duplicate_tower_code", $"Tower code {towerCode} already exists.");
        }

        var tower = new Tower
        {
            SiteId = siteId,
            TowerCode = towerCode,
            StructureType = request.StructureType,
            HeightMeters = request.HeightMeters,
            TenantCapacity = request.TenantCapacity,
            AntennaMountCapacity = request.AntennaMountCapacity,
            CommissionedOn = request.CommissionedOn
        };

        _db.Towers.Add(tower);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(tower.Id, cancellationToken);
    }

    public async Task<TowerDto> UpdateAsync(Guid id, UpdateTowerRequest request, CancellationToken cancellationToken)
    {
        var tower = await LoadTowers().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
                    ?? throw new KeyNotFoundException($"Tower {id} was not found.");

        var activeTenants = tower.Tenancies.Count(t => t.Status == TenancyStatus.Active);

        if (request.TenantCapacity < activeTenants)
        {
            throw new DomainRuleException(
                "capacity_below_active_tenancies",
                $"Tower {tower.TowerCode} already hosts {activeTenants} active tenant(s); capacity cannot be reduced below that.");
        }

        tower.StructureType = request.StructureType;
        tower.HeightMeters = request.HeightMeters;
        tower.TenantCapacity = request.TenantCapacity;
        tower.AntennaMountCapacity = request.AntennaMountCapacity;
        tower.CommissionedOn = request.CommissionedOn;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    private IQueryable<Tower> LoadTowers() => _db.Towers
        .Include(t => t.Site)
        .Include(t => t.Tenancies)
        .ThenInclude(t => t.Operator);
}
