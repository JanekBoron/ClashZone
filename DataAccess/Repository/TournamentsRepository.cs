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
    /// <summary>
    /// Provides methods for accessing tournament data.  It supports filtering
    /// upcoming tournaments by format and retrieving tournaments for a specific
    /// user.
    /// </summary>
    public class TournamentsRepository : ITournamentsRepository
    {
        private readonly ApplicationDbContext _context;

        public TournamentsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync(string? format)
        {
            var query = _context.Tournaments.AsQueryable();
            // Only upcoming tournaments
            query = query.Where(t => t.StartDate > DateTime.UtcNow);
            // Optional filter by format (team size)
            if (!string.IsNullOrEmpty(format))
            {
                query = query.Where(t => t.Format == format);
            }
            return await query.OrderBy(t => t.StartDate).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Tournament>> GetUserTournamentsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Array.Empty<Tournament>();
            }
            // If you have a UserTournaments join table: return tournaments where the
            // user is a participant.  Otherwise fall back to tournaments created
            // by the user.
            // Example pivot query (requires a DbSet<UserTournament> with
            // navigation properties):
            // return await _context.UserTournaments
            //    .Where(ut => ut.UserId == userId)
            //    .Select(ut => ut.Tournament)
            //    .OrderBy(t => t.StartDate)
            //    .ToListAsync();

            return await _context.Tournaments
                .Where(t => t.CreatedByUserId == userId)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
        }
    }
}
