namespace Tms.Api.Auth;

/// <summary>
/// Fine-grained capabilities derived from roles. The API authorises on these (as policies)
/// and the React app uses the same names to decide which menu entries and buttons to show,
/// so authorisation and UI visibility can never drift apart.
/// </summary>
public static class Permissions
{
    public const string PortfolioView = "Portfolio.View";
    public const string DashboardView = "Dashboard.View";
    public const string SitesManage = "Sites.Manage";
    public const string TowersManage = "Towers.Manage";
    public const string OperatorsManage = "Operators.Manage";
    public const string TenanciesManage = "Tenancies.Manage";
    public const string UsersManage = "Users.Manage";

    public static readonly string[] All =
    [
        PortfolioView,
        DashboardView,
        SitesManage,
        TowersManage,
        OperatorsManage,
        TenanciesManage,
        UsersManage
    ];

    public static IReadOnlyList<string> ForRoles(IEnumerable<string> roles) =>
        roles.SelectMany(Map).Distinct().OrderBy(p => p, StringComparer.Ordinal).ToList();

    private static string[] Map(string role) => role switch
    {
        AppRoles.Admin => All,
        AppRoles.Operations => [PortfolioView, DashboardView, SitesManage, TowersManage],
        AppRoles.Commercial => [PortfolioView, DashboardView, OperatorsManage, TenanciesManage],
        AppRoles.Manager => [PortfolioView, DashboardView],
        _ => []
    };
}
