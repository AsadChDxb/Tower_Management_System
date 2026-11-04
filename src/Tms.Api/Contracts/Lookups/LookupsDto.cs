namespace Tms.Api.Contracts.Lookups;

public record LookupItem(string Value, string Label);

/// <summary>Single round-trip for every enum and reference list the UI needs to render forms.</summary>
public class LookupsDto
{
    public IReadOnlyList<LookupItem> Countries { get; init; } = [];

    public IReadOnlyList<LookupItem> SiteTypes { get; init; } = [];

    public IReadOnlyList<LookupItem> SiteStatuses { get; init; } = [];

    public IReadOnlyList<LookupItem> TowerStructureTypes { get; init; } = [];

    public IReadOnlyList<LookupItem> SpaceTypes { get; init; } = [];

    public IReadOnlyList<LookupItem> TenancyStatuses { get; init; } = [];

    public IReadOnlyList<LookupItem> Currencies { get; init; } = [];

    public IReadOnlyList<LookupItem> Roles { get; init; } = [];

    public IReadOnlyList<LookupItem> Permissions { get; init; } = [];

    /// <summary>Allowed site status transitions - lets the UI disable impossible actions.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SiteStatusTransitions { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();
}
