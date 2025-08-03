using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClashZone.DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClashZone.DataAccess.DbInitializer
{
    public static class RoleSeeder
    {
        private static readonly string[] Roles = new[]
        {
            "Player",
            "Organizer",
            "Admin"
        };

        /// <summary>
        /// Seed roles and a default administrator user.  The admin user has a
        /// well‑known e‑mail address and password which should be changed at
        /// deployment time.  Assigning the Admin role enables this account to
        /// manage other users and grant roles.
        /// </summary>
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ClashUser>>();

            // Ensure roles exist
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create default admin account if it doesn't exist
            const string adminEmail = "admin@clashzone.local";
            const string adminPassword = "Admin#123"; // TODO: override from configuration

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ClashUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = "System Administrator"
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Failed to create default admin user: " + string.Join(", ", result.Errors));
                }
            }
            // Ensure admin user has the Admin role
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
