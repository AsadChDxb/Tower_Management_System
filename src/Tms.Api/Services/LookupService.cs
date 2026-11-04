using System.Text.RegularExpressions;
using Tms.Api.Auth;
using Tms.Api.Contracts.Lookups;
using Tms.Domain.Enums;
using Tms.Domain.Rules;

namespace Tms.Api.Services;

public interface ILookupService
{
    LookupsDto Get();
}

/// <summary>Reference data for the UI forms, plus allowed site status transitions.</summary>
public class LookupService : ILookupService
{
    private static readonly (string Code, string Name)[] CountryList =
    [
        ("DE", "Germany"),
        ("NL", "Netherlands"),
        ("FR", "France"),
        ("ES", "Spain"),
        ("PL", "Poland"),
        ("GB", "United Kingdom"),
        ("BE", "Belgium"),
        ("AT", "Austria")
    ];

    private static readonly (string Code, string Name)[] CurrencyList =
    [
        ("EUR", "Euro (EUR)"),
        ("GBP", "Pound sterling (GBP)"),
        ("PLN", "Polish zloty (PLN)"),
        ("USD", "US dollar (USD)")
    ];

    public LookupsDto Get() => new()
    {
        Countries = CountryList.Select(c => new LookupItem(c.Code, c.Name)).ToList(),
        Currencies = CurrencyList.Select(c => new LookupItem(c.Code, c.Name)).ToList(),
        SiteTypes = ToLookups<SiteType>(),
        SiteStatuses = ToLookups<SiteStatus>(),
        TowerStructureTypes = ToLookups<TowerStructureType>(),
        SpaceTypes = ToLookups<SpaceType>(),
        TenancyStatuses = ToLookups<TenancyStatus>(),
        Roles = AppRoles.All.Select(role => new LookupItem(role, role)).ToList(),
        Permissions = Permissions.All.Select(permission => new LookupItem(permission, permission)).ToList(),
        SiteStatusTransitions = Enum.GetValues<SiteStatus>().ToDictionary(
            status => status.ToString(),
            status => (IReadOnlyList<string>)SiteStatusRules.AllowedNextStates(status).Select(s => s.ToString()).ToList())
    };

    private static List<LookupItem> ToLookups<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Select(value =>
            {
                var name = value.ToString();
                return new LookupItem(name, Regex.Replace(name, "(?<!^)([A-Z])", " $1"));
            })
            .ToList();
}
