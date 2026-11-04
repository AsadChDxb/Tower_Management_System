using Microsoft.EntityFrameworkCore;
using Tms.Api.Contracts.Operators;
using Tms.Domain.Common;
using Tms.Domain.Entities;
using Tms.Domain.Enums;
using Tms.Infrastructure.Persistence;

namespace Tms.Api.Services;

public interface IOperatorService
{
    Task<IReadOnlyList<OperatorDto>> ListAsync(string? search, CancellationToken cancellationToken);

    Task<OperatorDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<OperatorDto> CreateAsync(SaveOperatorRequest request, CancellationToken cancellationToken);

    Task<OperatorDto> UpdateAsync(Guid id, SaveOperatorRequest request, CancellationToken cancellationToken);
}

public class OperatorService : IOperatorService
{
    private readonly TmsDbContext _db;

    public OperatorService(TmsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OperatorDto>> ListAsync(string? search, CancellationToken cancellationToken)
    {
        var operators = _db.Operators.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            operators = operators.Where(o => o.Name.Contains(term) || o.CountryCode == term.ToUpper());
        }

        return await operators
            .OrderBy(o => o.Name)
            .Select(o => new OperatorDto
            {
                Id = o.Id,
                Name = o.Name,
                CountryCode = o.CountryCode,
                ContactEmail = o.ContactEmail,
                ContactPhone = o.ContactPhone,
                CreatedAtUtc = o.CreatedAtUtc,
                ActiveTenancies = o.Tenancies.Count(t => t.Status == TenancyStatus.Active),
                MonthlyRevenue = o.Tenancies
                    .Where(t => t.Status == TenancyStatus.Active)
                    .Sum(t => (decimal?)t.MonthlyRent) ?? 0m
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OperatorDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var operators = await ListAsync(null, cancellationToken);

        return operators.FirstOrDefault(o => o.Id == id)
               ?? throw new KeyNotFoundException($"Operator {id} was not found.");
    }

    public async Task<OperatorDto> CreateAsync(SaveOperatorRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await _db.Operators.AnyAsync(o => o.Name == name, cancellationToken))
        {
            throw new DomainRuleException("duplicate_operator_name", $"An operator named '{name}' already exists.");
        }

        var entity = new Operator
        {
            Name = name,
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            ContactEmail = request.ContactEmail?.Trim(),
            ContactPhone = request.ContactPhone?.Trim()
        };

        _db.Operators.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<OperatorDto> UpdateAsync(Guid id, SaveOperatorRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.Operators.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                     ?? throw new KeyNotFoundException($"Operator {id} was not found.");

        var name = request.Name.Trim();

        if (await _db.Operators.AnyAsync(o => o.Name == name && o.Id != id, cancellationToken))
        {
            throw new DomainRuleException("duplicate_operator_name", $"An operator named '{name}' already exists.");
        }

        entity.Name = name;
        entity.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        entity.ContactEmail = request.ContactEmail?.Trim();
        entity.ContactPhone = request.ContactPhone?.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }
}
