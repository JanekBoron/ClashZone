using ClashZone.DataAccess.Models;
using DataAccess;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.DbInitializer
{
    /// <summary>
    /// Klasa odpowiedzialna za inicjalizację bazy danych.  Wykonuje migracje,
    /// seedy ról oraz przykładowe dane.  Dodano również seeding planów
    /// subskrypcyjnych aby można było korzystać z pakietów premium oraz ligi.
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Wykonuje migracje, seeduje role/admina i wstawia przykładowe turnieje
        /// oraz plany subskrypcyjne.  Metoda jest idempotentna – nie duplikuje
        /// danych przy wielokrotnym wywołaniu.
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider services)
        {
            // Utwórz scope aby rozwiązać zależności o zasięgu scoped
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Zapewnij, że baza danych istnieje i zastosuj migracje
            await context.Database.MigrateAsync();
            // Seeduj role i administratora
            await RoleSeeder.SeedAsync(scope.ServiceProvider);
            // Seed przykładowych turniejów
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
                        CreatedByUserId = null,
                        IsPublic = true,
                        IsPremium = false
                    },
                    new Tournament
                    {
                        Name = "LoL 1v1 Duel",
                        GameTitle = "League of Legends",
                        StartDate = DateTime.UtcNow.AddMonths(2),
                        MaxParticipants = 32,
                        Format = "1v1",
                        Prize = "100 EUR",
                        CreatedByUserId = null,
                        IsPublic = true,
                        IsPremium = false
                    }
                };
                context.AddRange(tournaments);
                await context.SaveChangesAsync();
            }
            // Seed planów subskrypcyjnych
            if (!context.Set<SubscriptionPlan>().Any())
            {
                var plans = new[]
                {
                    new SubscriptionPlan
                    {
                        Name = "Premium",
                        Price = 10m,
                        IsPremiumAccess = true,
                        IsLeagueAccess = false,
                        DurationDays = 30
                    },
                    new SubscriptionPlan
                    {
                        Name = "League",
                        Price = 10m,
                        IsPremiumAccess = false,
                        IsLeagueAccess = true,
                        DurationDays = 30
                    },
                    new SubscriptionPlan
                    {
                        Name = "Ultra",
                        Price = 18.99m,
                        IsPremiumAccess = true,
                        IsLeagueAccess = true,
                        DurationDays = 30
                    }
                };
                context.AddRange(plans);
                await context.SaveChangesAsync();
            }
        }
    }

}
