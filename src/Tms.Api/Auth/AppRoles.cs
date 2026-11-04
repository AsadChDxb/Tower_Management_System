namespace Tms.Api.Auth;

/// <summary>The four application roles. Kept in one place so seeding, policies and DTOs agree.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Operations = "Operations";
    public const string Commercial = "Commercial";
    public const string Manager = "Manager";

    public static readonly string[] All = [Admin, Operations, Commercial, Manager];
}
