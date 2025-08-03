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
    /// Application database context.  Inherits from <see
    /// cref="IdentityDbContext{TUser}"/> to include ASP.NET Core Identity
    /// tables for authentication and authorization.  Additional DbSet
    /// properties are defined for tournaments, teams and team members.
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
        /// Collection of teams participating in tournaments.
        /// </summary>
        public DbSet<Team> Teams { get; set; }

        /// <summary>
        /// Collection linking users to teams.
        /// </summary>
        public DbSet<TeamMember> TeamMembers { get; set; }

        /// <summary>
        /// Collection of chat messages posted in tournaments.
        /// </summary>
        public DbSet<ChatMessage> ChatMessages { get; set; }

        /*
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Additional configuration can be placed here.
        }
        */
    }
}
