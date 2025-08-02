using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Models;
using DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.DataAccess.DbInitializer
{
    public static class DbInitializer
    {
        /// <summary>
        /// Execute migrations, seed roles/users via RoleSeeder and insert
        /// example tournaments.  This method is idempotent – it will not
        /// duplicate data if called multiple times.
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider services)
        {
            // Create a scope so we can resolve scoped services like DbContext
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure the database exists and apply pending migrations.  This is
            // important because the seeding logic depends on the schema being up
            // to date.
            await context.Database.MigrateAsync();

            // Seed roles and admin user using the RoleSeeder defined in IdentitySetup.cs
            await RoleSeeder.SeedAsync(scope.ServiceProvider);

            // Seed example tournaments if the table is empty.  Replace the
            // properties with those defined in your actual Tournament entity.  If
            // you prefer raw SQL inserts, you can call context.Database.ExecuteSqlRaw().
            if (!context.Set<Tournament>().Any())
            {
                var tournaments = new[]
                {
                    new Tournament
                    {
                        Name = "Summer CS2 Cup",
                        GameTitle = "Counter Strike 2",
                        StartDate = DateTime.UtcNow.AddMonths(1),
                        MaxParticipants = 16,
                        Format = "5v5",
                        Prize = "500 EUR",
                        CreatedByUserId = null // assign the organizer user ID here
                    },
                    new Tournament
                    {
                        Name = "LoL 1v1 Duel",
                        GameTitle = "League of Legends",
                        StartDate = DateTime.UtcNow.AddMonths(2),
                        MaxParticipants = 32,
                        Format = "1v1",
                        Prize = "100 EUR",
                        CreatedByUserId = null
                    }
                };
                context.AddRange(tournaments);
                await context.SaveChangesAsync();
            }
        }
    }

}
