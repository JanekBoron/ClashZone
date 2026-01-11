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
    /// <summary>
    /// Application database context.  Inherits from <see cref="IdentityDbContext{TUser}"/>
    /// to integrate Identity tables with domain-specific tables.  This class
    /// exposes <see cref="DbSet"/> properties for all persistent entities used
    /// throughout the application including tournaments, teams, chat messages,
    /// subscriptions, products and newly added match and statistics entities.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ClashUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Parameterless constructor used by tooling; do not remove.
        public ApplicationDbContext() { }

        public DbSet<Tournament> Tournaments { get; set; } = null!;
        /// <summary>
        /// Collection of teams participating in tournaments.
        /// </summary>
        public DbSet<Team> Teams { get; set; } = null!;
        /// <summary>
        /// Association between users and teams.
        /// </summary>
        public DbSet<TeamMember> TeamMembers { get; set; } = null!;
        /// <summary>
        /// Chat messages published in tournaments.
        /// </summary>
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        /// <summary>
        /// Subscription plans available to purchase.
        /// </summary>
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        /// <summary>
        /// Subscriptions purchased by users.
        /// </summary>
        public DbSet<UserSubscription> UserSubscriptions { get; set; } = null!;

        // ClashCoins tables
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductCategory> ProductCategories { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<CoinWallet> CoinWallets { get; set; } = null!;
        public DbSet<CoinWalletTransaction> CoinWalletTransactions { get; set; } = null!;
        public DbSet<ProductRedeem> ProductRedeems { get; set; } = null !;

        // New entities for match results and statistics
        public DbSet<Match> Matches { get; set; } = null!;
        public DbSet<PlayerMatchStat> PlayerMatchStats { get; set; } = null!;
        public DbSet<UserStat> UserStats { get; set; } = null!;
    }
}