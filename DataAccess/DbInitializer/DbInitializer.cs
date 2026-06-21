using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.DataAccess.Models;
using DataAccess;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.DbInitializer
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            await RoleSeeder.SeedAsync(scope.ServiceProvider);
            if (!context.Set<Tournament>().Any())
            {
                var tournaments = new[]
                {
                    new Tournament
                    {
                        Name = "Summer CS2 Cup",
                        GameTitle = "Counter Strike 2",
                        StartDate = DateTime.UtcNow.AddMonths(1),
                        MaxParticipants = 6,
                        Format = "5v5",
                        FirstPlacePrize = "500 EUR",
                        SecondPlacePrize = "250 EUR",
                        ThirdPlacePrize = "100 EUR",
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
                        FirstPlacePrize = "500 EUR",
                        SecondPlacePrize = "250 EUR",
                        ThirdPlacePrize = "100 EUR",
                        CreatedByUserId = null,
                        IsPublic = true,
                        IsPremium = false
                    },
                    new Tournament
                    {
                        Name = "LoL Riot API Test",
                        GameTitle = "League of Legends",
                        StartDate = DateTime.UtcNow.AddDays(7),
                        MaxParticipants = 2,
                        Format = "5v5",
                        FirstPlacePrize = "1000 ClashCoins",
                        SecondPlacePrize = "500 ClashCoins",
                        ThirdPlacePrize = string.Empty,
                        CreatedByUserId = null,
                        IsPublic = true,
                        IsPremium = false
                    },
                    new Tournament
                    {
                        Name = "Spring CS2 Championship 2026",
                        GameTitle = "Counter Strike 2",
                        StartDate = DateTime.UtcNow.AddDays(-14),
                        MaxParticipants = 8,
                        Format = "5v5",
                        FirstPlacePrize = "2000 ClashCoins",
                        SecondPlacePrize = "1000 ClashCoins",
                        ThirdPlacePrize = "500 ClashCoins",
                        CreatedByUserId = null,
                        IsPublic = true,
                        IsPremium = false
                    }
                };
                context.AddRange(tournaments);
                await context.SaveChangesAsync();
            }
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
            if (!context.Set<Season>().Any())
            {
                var season = new Season
                {
                    Name = "ClashZone League - Sezon 1",
                    Game = "Counter Strike 2",
                    Format = "5v5",
                    RegistrationOpenDate = DateTime.UtcNow.AddDays(-1),
                    RegistrationCloseDate = DateTime.UtcNow.AddDays(14),
                    StartDate = DateTime.UtcNow.AddDays(16),
                    RosterSize = 5,
                    PlayoffSpots = 4,
                    FirstPlacePrize = "1000 ClashCoins",
                    SecondPlacePrize = "500 ClashCoins",
                    ThirdPlacePrize = "250 ClashCoins",
                    DefaultMatchDay = "Środa 20:00 CEST",
                    Status = 0,
                    CreatedByUserId = null
                };
                context.Add(season);
                await context.SaveChangesAsync();
            }
            if (!context.Set<Product>().Any())
            {
                var category = new ProductCategory
                {
                    Name = "Peryferia",
                    Slug = "peryferia"
                };

                var products = new[]
                {
                    new Product
                    {
                        Name = "Słuchawki gamingowe ClashZone",
                        ShortDescription = "Słuchawki z dźwiękiem przestrzennym i mikrofonem.",
                        LongDescription = "Wygodne słuchawki gamingowe z dźwiękiem przestrzennym 7.1, "
                            + "redukcją szumów i odpinanym mikrofonem. Idealne na długie sesje.",
                        ClashCoins = 1500,
                        LimitedEdition = false,
                        Stock = 25,
                        IsActive = true,
                        Category = category,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { Url = "/images/HeadSet.png", IsPrimary = true, SortOrder = 0 }
                        }
                    },
                    new Product
                    {
                        Name = "Mysz gamingowa ClashZone",
                        ShortDescription = "Lekka mysz z sensorem 16 000 DPI.",
                        LongDescription = "Precyzyjna mysz gamingowa z sensorem optycznym 16 000 DPI, "
                            + "programowalnymi przyciskami i ergonomicznym kształtem.",
                        ClashCoins = 1200,
                        LimitedEdition = false,
                        Stock = 40,
                        IsActive = true,
                        Category = category,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { Url = "/images/Mouse.png", IsPrimary = true, SortOrder = 0 }
                        }
                    },
                    new Product
                    {
                        Name = "Klawiatura mechaniczna ClashZone",
                        ShortDescription = "Mechaniczna klawiatura z podświetleniem RGB.",
                        LongDescription = "Mechaniczna klawiatura gamingowa z przełącznikami liniowymi, "
                            + "pełnym podświetleniem RGB i anti-ghostingiem (N-key rollover).",
                        ClashCoins = 2000,
                        LimitedEdition = true,
                        Stock = 15,
                        IsActive = true,
                        Category = category,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { Url = "/images/Keyboard.png", IsPrimary = true, SortOrder = 0 }
                        }
                    }
                };
                context.AddRange(products);
                await context.SaveChangesAsync();
            }
            if (!context.Set<Team>().Any())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ClashUser>>();
                var nextUserIndex = 1;

                var csTournament = context.Set<Tournament>()
                    .First(t => t.Name == "Summer CS2 Cup");
                nextUserIndex = await SeedTournamentTeamsAsync(
                    context, userManager, csTournament.Id,
                    teamCount: 6, rosterSize: 5, userStartIndex: nextUserIndex);

                var lolTournament = context.Set<Tournament>()
                    .First(t => t.Name == "LoL Riot API Test");
                nextUserIndex = await SeedTournamentTeamsAsync(
                    context, userManager, lolTournament.Id,
                    teamCount: 2, rosterSize: 5, userStartIndex: nextUserIndex);

                var finishedTournament = context.Set<Tournament>()
                    .First(t => t.Name == "Spring CS2 Championship 2026");
                nextUserIndex = await SeedTournamentTeamsAsync(
                    context, userManager, finishedTournament.Id,
                    teamCount: 8, rosterSize: 5, userStartIndex: nextUserIndex);
            }
            if (!context.Set<LeagueRegistration>().Any())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ClashUser>>();
                var season = context.Set<Season>().First();
                await SeedLeagueTeamsAsync(
                    context, userManager, season,
                    teamCount: 6, userStartIndex: 81);
            }
            if (!context.Set<UserStat>().Any())
            {
                var finishedTournament = context.Set<Tournament>()
                    .FirstOrDefault(t => t.Name == "Spring CS2 Championship 2026");
                if (finishedTournament != null)
                {
                    var teamCount = context.Set<Team>()
                        .Count(t => t.TournamentId == finishedTournament.Id && t.IsConfirmed);
                    var bracketService = scope.ServiceProvider.GetRequiredService<IBracketService>();
                    await PlayOutTournamentAsync(bracketService, finishedTournament.Id, teamCount);
                }
            }
            if (!context.Set<ProductRedeem>().Any())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ClashUser>>();
                var coinWallet = scope.ServiceProvider.GetRequiredService<ICoinWalletService>();
                var coinShop = scope.ServiceProvider.GetRequiredService<ICoinShopService>();
                var redeemRepo = scope.ServiceProvider.GetRequiredService<IProductRedeemRepository>();
                await SeedSampleOrdersAsync(context, userManager, coinWallet, coinShop, redeemRepo);
            }
        }

        private static async Task PlayOutTournamentAsync(
            IBracketService bracketService, int tournamentId, int teamCount)
        {
            if (teamCount < 2) return;
            int rounds = (int)Math.Ceiling(Math.Log2(teamCount));
            for (int round = 1; round <= rounds; round++)
            {
                int matchesInRound = (int)Math.Ceiling(teamCount / Math.Pow(2, round));
                for (int match = 1; match <= matchesInRound; match++)
                {
                    await bracketService.SimulateMatchAsync(tournamentId, round, match, isAdmin: false);
                }
            }
        }

        private static async Task SeedSampleOrdersAsync(
            ApplicationDbContext context, UserManager<ClashUser> userManager,
            ICoinWalletService coinWallet, ICoinShopService coinShop,
            IProductRedeemRepository redeemRepo)
        {
            var products = context.Set<Product>().OrderBy(p => p.Id).ToList();
            if (products.Count == 0) return;

            var orderPlan = new[]
            {
                ("user1",  new[] { "Słuchawki gamingowe ClashZone", "Mysz gamingowa ClashZone" }),
                ("user2",  new[] { "Klawiatura mechaniczna ClashZone" }),
                ("user3",  new[] { "Mysz gamingowa ClashZone" }),
                ("user41", new[] { "Słuchawki gamingowe ClashZone" }),
                ("user42", new[] { "Klawiatura mechaniczna ClashZone" })
            };

            var addresses = new Dictionary<string, ShippingAddressDomain>
            {
                ["user1"]  = new() { FullName = "Jan Kowalski",   Phone = "+48 600 100 200", Street = "ul. Marszałkowska 12/4", PostalCode = "00-590", City = "Warszawa", Country = "Polska" },
                ["user2"]  = new() { FullName = "Anna Nowak",      Phone = "+48 601 222 333", Street = "ul. Długa 7",           PostalCode = "80-827", City = "Gdańsk",   Country = "Polska" },
                ["user3"]  = new() { FullName = "Piotr Wiśniewski",Phone = "+48 602 444 555", Street = "ul. Floriańska 21",     PostalCode = "31-019", City = "Kraków",   Country = "Polska" },
                ["user41"] = new() { FullName = "Michał Lewandowski", Phone = "+48 603 666 777", Street = "ul. Piotrkowska 100", PostalCode = "90-001", City = "Łódź",     Country = "Polska" },
                ["user42"] = new() { FullName = "Katarzyna Zielińska", Phone = "+48 604 888 999", Street = "ul. Świdnicka 5",   PostalCode = "50-068", City = "Wrocław",  Country = "Polska" }
            };

            foreach (var (userName, productNames) in orderPlan)
            {
                var user = await userManager.FindByNameAsync(userName);
                if (user == null) continue;
                await coinWallet.CreditAsync(user.Id, 5000, "Seed:WelcomeBonus");

                var shipping = addresses.TryGetValue(userName, out var addr)
                    ? addr
                    : new ShippingAddressDomain { FullName = userName, Phone = "+48 600 000 000", Street = "ul. Testowa 1", PostalCode = "00-000", City = "Warszawa", Country = "Polska" };

                foreach (var productName in productNames)
                {
                    var product = products.FirstOrDefault(p => p.Name == productName);
                    if (product == null) continue;
                    await coinShop.PurchaseAsync(product.Id, user.Id, shipping);
                }
            }

            var statuses = new[]
            {
                OrderStatus.Completed, OrderStatus.Processing,
                OrderStatus.Pending, OrderStatus.Cancelled
            };
            var allOrders = await redeemRepo.GetAllAsync();
            for (int i = 0; i < allOrders.Count; i++)
            {
                await redeemRepo.UpdateStatusAsync(allOrders[i].Id, statuses[i % statuses.Length], null);
            }
        }

        private static async Task<int> SeedTournamentTeamsAsync(
            ApplicationDbContext context, UserManager<ClashUser> userManager,
            int tournamentId, int teamCount, int rosterSize, int userStartIndex)
        {
            var index = userStartIndex;
            for (int t = 1; t <= teamCount; t++)
            {
                var memberIds = new List<string>();
                for (int r = 0; r < rosterSize; r++)
                {
                    memberIds.Add(await EnsureUserAsync(userManager, $"user{index}"));
                    index++;
                }

                var team = new Team
                {
                    TournamentId = tournamentId,
                    CaptainId = memberIds[0],
                    Name = $"Team {t}",
                    JoinCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    IsConfirmed = true
                };
                context.Add(team);
                await context.SaveChangesAsync();

                foreach (var userId in memberIds)
                {
                    context.Add(new TeamMember { TeamId = team.Id, UserId = userId, IsReady = true });
                }
                await context.SaveChangesAsync();
            }
            return index;
        }

        private static async Task<int> SeedLeagueTeamsAsync(
            ApplicationDbContext context, UserManager<ClashUser> userManager,
            Season season, int teamCount, int userStartIndex)
        {
            var index = userStartIndex;
            var rosterSize = season.RosterSize > 0 ? season.RosterSize : 5;
            for (int t = 1; t <= teamCount; t++)
            {
                var memberIds = new List<string>();
                for (int r = 0; r < rosterSize; r++)
                {
                    memberIds.Add(await EnsureUserAsync(userManager, $"user{index}"));
                    index++;
                }

                var leagueTeam = new LeagueTeam
                {
                    Name = $"Liga Team {t}",
                    CaptainId = memberIds[0],
                    JoinCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    Game = season.Game,
                    CreatedAt = DateTime.UtcNow
                };
                context.Add(leagueTeam);
                await context.SaveChangesAsync();

                foreach (var userId in memberIds)
                {
                    context.Add(new LeagueTeamMember
                    {
                        LeagueTeamId = leagueTeam.Id,
                        UserId = userId,
                        JoinedAt = DateTime.UtcNow
                    });
                }
                await context.SaveChangesAsync();

                context.Add(new LeagueRegistration
                {
                    SeasonId = season.Id,
                    LeagueTeamId = leagueTeam.Id,
                    RegisteredAt = DateTime.UtcNow,
                    FeePaid = true,
                    IsRemoved = false
                });
                await context.SaveChangesAsync();
            }
            return index;
        }

        private static async Task<string> EnsureUserAsync(UserManager<ClashUser> userManager, string userName)
        {
            var existing = await userManager.FindByNameAsync(userName);
            if (existing != null)
            {
                return existing.Id;
            }

            var user = new ClashUser
            {
                UserName = userName,
                Email = $"{userName}@clashzone.local",
                EmailConfirmed = true,
                DisplayName = userName
            };
            var result = await userManager.CreateAsync(user, "User#123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create seed user {userName}: "
                    + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            await userManager.AddToRoleAsync(user, "Player");
            return user.Id;
        }
    }

}
