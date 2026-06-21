using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClashZone.DataAccess.Models;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ClashUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public ApplicationDbContext() { }

        public DbSet<Tournament> Tournaments { get; set; } = null!;
        public DbSet<Team> Teams { get; set; } = null!;
        public DbSet<TeamMember> TeamMembers { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<UserSubscription> UserSubscriptions { get; set; } = null!;

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductCategory> ProductCategories { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<CoinWallet> CoinWallets { get; set; } = null!;
        public DbSet<CoinWalletTransaction> CoinWalletTransactions { get; set; } = null!;
        public DbSet<ProductRedeem> ProductRedeems { get; set; } = null !;

        public DbSet<Match> Matches { get; set; } = null!;
        public DbSet<PlayerMatchStat> PlayerMatchStats { get; set; } = null!;
        public DbSet<UserStat> UserStats { get; set; } = null!;

        public DbSet<LeagueTeam> LeagueTeams { get; set; } = null!;
        public DbSet<LeagueTeamMember> LeagueTeamMembers { get; set; } = null!;
        public DbSet<Season> Seasons { get; set; } = null!;
        public DbSet<LeagueRegistration> LeagueRegistrations { get; set; } = null!;
        public DbSet<LeagueMatch> LeagueMatches { get; set; } = null!;
    }
}