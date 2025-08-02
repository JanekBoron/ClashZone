using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClashZone.DataAccess.Repository.Interfaces;
using DataAccess;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.DataAccess.Repository
{
    public class TournamentsRepository : ITournamentsRepository
    {
        private readonly ApplicationDbContext _context;
        public TournamentsRepository(ApplicationDbContext context)
        {
               _context = context;
        }
        public async Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync()
        {
            return await _context.Tournaments
                .Where(t => t.StartDate > DateTime.UtcNow)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
        }
    }
}
