using Microsoft.AspNetCore.Authorization;

namespace Tms.Api.Auth;

/// <summary>
/// Maps permissions to roles once, at startup. Controllers only ever reference a permission,
/// so changing who can do what is a one-line change here.
/// </summary>
public static class AuthorizationPolicies
{
    public static IServiceCollection AddTmsAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Permissions.PortfolioView, policy => policy
                .RequireRole(AppRoles.Admin, AppRoles.Operations, AppRoles.Commercial, AppRoles.Manager))
            .AddPolicy(Permissions.DashboardView, policy => policy
                .RequireRole(AppRoles.Admin, AppRoles.Operations, AppRoles.Commercial, AppRoles.Manager))
            .AddPolicy(Permissions.SitesManage, policy => policy
                .RequireRole(AppRoles.Admin, AppRoles.Operations))
            .AddPolicy(Permissions.TowersManage, policy => policy
                .RequireRole(AppRoles.Admin, AppRoles.Operations))
            .AddPolicy(Permissions.OperatorsManage, policy => policy
                .RequireRole(AppRoles.Admin, AppRoles.Commercial))
            .AddPolicy(Permissions.TenanciesManage, policy => policy
                .RequireRole(AppRoles.Admin, AppRoles.Commercial))
            .AddPolicy(Permissions.UsersManage, policy => policy
                .RequireRole(AppRoles.Admin));

        return services;
    }
}
