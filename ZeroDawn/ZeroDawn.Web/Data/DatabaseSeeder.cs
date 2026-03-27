#nullable enable

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZeroDawn.Shared.Core.Constants;

namespace ZeroDawn.Web.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        // Seed roles
        string[] roles = [Roles.SuperAdmin, Roles.Admin, Roles.User];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        // Seed static super admin
        const string adminEmail = "loai.asp97@gmail.com";
        const string adminPassword = "012Shbl10@ZD";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Super Admin",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
                logger.LogInformation("Seeded static super admin account.");
            }
            else
            {
                logger.LogError("Failed to seed static super admin: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (admin is not null)
        {
            admin.UserName = adminEmail;
            admin.Email = adminEmail;
            admin.FullName = "Super Admin";
            admin.EmailConfirmed = true;
            admin.IsActive = true;

            await userManager.UpdateAsync(admin);

            if (!await userManager.IsInRoleAsync(admin, Roles.SuperAdmin))
            {
                await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
            }

            if (!await userManager.CheckPasswordAsync(admin, adminPassword))
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
                await userManager.ResetPasswordAsync(admin, resetToken, adminPassword);
            }
        }

        var legacyAdmin = await userManager.FindByEmailAsync("admin@zerodawn.local");
        if (legacyAdmin is not null && legacyAdmin.Id != admin?.Id)
        {
            if (await userManager.IsInRoleAsync(legacyAdmin, Roles.SuperAdmin))
            {
                await userManager.RemoveFromRoleAsync(legacyAdmin, Roles.SuperAdmin);
            }

            legacyAdmin.IsActive = false;
            await userManager.UpdateAsync(legacyAdmin);
        }
    }
}
