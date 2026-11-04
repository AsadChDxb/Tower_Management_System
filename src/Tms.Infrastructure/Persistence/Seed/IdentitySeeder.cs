using Microsoft.AspNetCore.Identity;
using Tms.Infrastructure.Identity;

namespace Tms.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates the four application roles and one demo user per role.
/// Demo credentials are deliberately simple and documented in the README.
/// </summary>
public static class IdentitySeeder
{
    public const string DemoPassword = "TowerCo!2026";

    public static readonly string[] Roles = ["Admin", "Operations", "Commercial", "Manager"];

    private static readonly (string Email, string DisplayName, string Role, string Phone)[] DemoUsers =
    [
        ("admin@towerco.demo", "Amina Admin", "Admin", "+49 30 5550 100"),
        ("ops@towerco.demo", "Omar Operations", "Operations", "+49 30 5550 200"),
        ("commercial@towerco.demo", "Clara Commercial", "Commercial", "+49 30 5550 300"),
        ("manager@towerco.demo", "Mehmet Manager", "Manager", "+49 30 5550 400")
    ];

    public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager, UserManager<AppUser> userManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        foreach (var (email, displayName, role, phone) in DemoUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                PhoneNumber = phone,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(user, DemoPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Seeding demo user {email} failed: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(user, role);
        }
    }
}
