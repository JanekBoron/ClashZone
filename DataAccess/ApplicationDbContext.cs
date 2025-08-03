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
    public class ApplicationDbContext : IdentityDbContext<ClashUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public ApplicationDbContext() { }

        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Team> Teams { get; set; } 
        public DbSet<TeamMember> TeamMembers { get; set; } 

        /*        protected override void OnModelCreating(ModelBuilder modelBuilder)
                { 
                    base.OnModelCreating(modelBuilder);
                }*/
    }
}
