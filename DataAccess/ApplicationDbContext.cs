using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClashZone.DataAccess.Models;

namespace DataAccess
{
    /// <summary>
    /// Application database context.  Dziedziczy po IdentityDbContext aby
    /// umożliwić integrację tabel użytkowników i ról z dodatkowymi tabelami
    /// domenowymi systemu.  Zawiera zbiory dla turniejów, drużyn, członków,
    /// wiadomości czatu oraz nowe zbiory subskrypcji.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ClashUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Parameterless constructor used by tools; do not remove
        public ApplicationDbContext() { }

        public DbSet<Tournament> Tournaments { get; set; }
        /// <summary>
        /// Kolekcja drużyn biorących udział w turniejach.
        /// </summary>
        public DbSet<Team> Teams { get; set; }
        /// <summary>
        /// Powiązanie użytkowników z drużynami.
        /// </summary>
        public DbSet<TeamMember> TeamMembers { get; set; }
        /// <summary>
        /// Wiadomości czatu publikowane w turniejach.
        /// </summary>
        public DbSet<ChatMessage> ChatMessages { get; set; }
        /// <summary>
        /// Kolekcja dostępnych planów subskrypcji.  Plany definiują poziomy
        /// dostępu (premium, liga, ultra) oraz cenę miesięczną.
        /// </summary>
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        /// <summary>
        /// Subskrypcje wykupione przez użytkowników wraz z datą ważności.
        /// </summary>
        public DbSet<UserSubscription> UserSubscriptions { get; set; }

        //ClashCoins
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<CoinWallet> CoinWallets { get; set; }
        public DbSet<CoinWalletTransaction> CoinWalletTransactions { get; set; }
        public DbSet<ProductRedeem> ProductRedeems { get; set; }

    }
}
