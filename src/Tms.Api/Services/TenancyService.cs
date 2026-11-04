using Microsoft.EntityFrameworkCore;
using Tms.Api.Contracts.Common;
using Tms.Api.Contracts.Sites;
using Tms.Api.Contracts.Tenancies;
using Tms.Domain.Common;
using Tms.Domain.Entities;
using Tms.Domain.Enums;
using Tms.Domain.Rules;
using Tms.Infrastructure.Persistence;

namespace Tms.Api.Services;

public interface ITenancyService
{
    Task<PagedResult<TenancyDto>> ListAsync(TenancyQuery query, CancellationToken cancellationToken);

    Task<TenancyDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<TenancyDto> CreateAsync(CreateTenancyRequest request, CancellationToken cancellationToken);

    Task<TenancyDto> UpdateAsync(Guid id, UpdateTenancyRequest request, CancellationToken cancellationToken);

    Task<TenancyDto> TerminateAsync(Guid id, TerminateTenancyRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Use case 2: assign an operator to a tower, guarded by the capacity, duplicate,
/// site-status and date rules from <see cref="TenancyRules"/>.
/// </summary>
public class TenancyService : ITenancyService
{
    private readonly TmsDbContext _db;

    public TenancyService(TmsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<TenancyDto>> ListAsync(TenancyQuery query, CancellationToken cancellationToken)
    {
        var tenancies = _db.Tenancies
            .AsNoTracking()
            .Include(t => t.Operator)
            .Include(t => t.Tower)
            .ThenInclude(t => t!.Site)
            .AsQueryable();

        if (query.TowerId.HasValue)
        {
            tenancies = tenancies.Where(t => t.TowerId == query.TowerId.Value);
        }

        if (query.OperatorId.HasValue)
        {
            tenancies = tenancies.Where(t => t.OperatorId == query.OperatorId.Value);
        }

        if (query.Status.HasValue)
        {
            tenancies = tenancies.Where(t => t.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var country = query.Country.Trim().ToUpperInvariant();
            tenancies = tenancies.Where(t => t.Tower!.Site!.CountryCode == country);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            tenancies = tenancies.Where(t =>
                t.Operator!.Name.Contains(term) ||
                t.Tower!.TowerCode.Contains(term) ||
                t.Tower!.Site!.SiteCode.Contains(term) ||
                t.Tower!.Site!.Name.Contains(term));
        }

        var totalCount = await tenancies.CountAsync(cancellationToken);

        var items = await tenancies
            .OrderByDescending(t => t.Status == TenancyStatus.Active)
            .ThenByDescending(t => t.StartDate)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TenancyDto>
        {
            Items = items.Select(PortfolioMapping.ToTenancyDto).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TenancyDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenancy = await LoadTenancies()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tenancy {id} was not found.");

        return PortfolioMapping.ToTenancyDto(tenancy);
    }

    public async Task<TenancyDto> CreateAsync(CreateTenancyRequest request, CancellationToken cancellationToken)
    {
        TenancyRules.EnsureValidTerm(request.StartDate, request.EndDate);
        TenancyRules.EnsureValidRent(request.MonthlyRent);

        // SqlServerRetryingExecutionStrategy requires that we run the manual transaction
        // through the execution strategy so a transient failure can be retried as a unit.
        var strategy = _db.Database.CreateExecutionStrategy();

        var tenancyId = await strategy.ExecuteAsync(
            async () => await CreateInTransactionAsync(request, cancellationToken));

        return await GetAsync(tenancyId, cancellationToken);
    }

    private async Task<Guid> CreateInTransactionAsync(CreateTenancyRequest request, CancellationToken cancellationToken)
    {
        // Serializable isolation plus the filtered unique index make the capacity check safe
        // against two commercial users grabbing the last slot at the same moment.
        await using var transaction = await _db.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        var tower = await _db.Towers
                        .Include(t => t.Site)
                        .Include(t => t.Tenancies)
                        .FirstOrDefaultAsync(t => t.Id == request.TowerId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Tower {request.TowerId} was not found.");

        var tenant = await _db.Operators.FirstOrDefaultAsync(o => o.Id == request.OperatorId, cancellationToken)
                     ?? throw new KeyNotFoundException($"Operator {request.OperatorId} was not found.");

        var activeTenants = tower.Tenancies.Count(t => t.Status == TenancyStatus.Active);

        TenancyRules.EnsureSiteIsLeasable(tower.Site!.Status, tower.Site.SiteCode);
        TenancyRules.EnsureCapacityAvailable(TenancyRules.Capacity(tower.TenantCapacity, activeTenants), tower.TowerCode);
        TenancyRules.EnsureNoDuplicateActiveTenancy(
            tower.Tenancies.Any(t => t.Status == TenancyStatus.Active && t.OperatorId == request.OperatorId),
            tower.TowerCode,
            tenant.Name);

        var tenancy = new Tenancy
        {
            TowerId = tower.Id,
            OperatorId = tenant.Id,
            SpaceType = request.SpaceType,
            AntennaCount = request.AntennaCount,
            MonthlyRent = request.MonthlyRent,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = TenancyStatus.Active,
            Notes = request.Notes
        };

        _db.Tenancies.Add(tenancy);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new DomainRuleException(
                "duplicate_active_tenancy",
                $"{tenant.Name} already has an active tenancy on tower {tower.TowerCode}.");
        }

        return tenancy.Id;
    }

    public async Task<TenancyDto> UpdateAsync(Guid id, UpdateTenancyRequest request, CancellationToken cancellationToken)
    {
        var tenancy = await LoadTenancies().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
                      ?? throw new KeyNotFoundException($"Tenancy {id} was not found.");

        TenancyRules.EnsureValidTerm(request.StartDate, request.EndDate);
        TenancyRules.EnsureValidRent(request.MonthlyRent);

        tenancy.SpaceType = request.SpaceType;
        tenancy.AntennaCount = request.AntennaCount;
        tenancy.MonthlyRent = request.MonthlyRent;
        tenancy.Currency = request.Currency.Trim().ToUpperInvariant();
        tenancy.StartDate = request.StartDate;
        tenancy.EndDate = request.EndDate;
        tenancy.Notes = request.Notes;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<TenancyDto> TerminateAsync(Guid id, TerminateTenancyRequest request, CancellationToken cancellationToken)
    {
        var tenancy = await LoadTenancies().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
                      ?? throw new KeyNotFoundException($"Tenancy {id} was not found.");

        TenancyRules.EnsureTerminable(tenancy.Status);

        var endDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        TenancyRules.EnsureValidTerm(tenancy.StartDate, endDate);

        tenancy.Status = TenancyStatus.Terminated;
        tenancy.EndDate = endDate;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            tenancy.Notes = request.Notes;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(tenancy.Id, cancellationToken);
    }

    private IQueryable<Tenancy> LoadTenancies() => _db.Tenancies
        .Include(t => t.Operator)
        .Include(t => t.Tower)
        .ThenInclude(t => t!.Site);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };
}
