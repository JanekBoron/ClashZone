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
    /// upcoming tournaments by format, retrieving tournaments for a specific
    /// user and fetching a single tournament by its identifier.
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
            // user is a participant.  Otherwise fall back to tournaments created by
            // the user.
            return await _context.Tournaments
                .Where(t => t.CreatedByUserId == userId)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task AddTournamentAsync(Tournament tournament)
        {
            if (tournament == null)
                throw new ArgumentNullException(nameof(tournament));

            // If the tournament is private, generate a join code
            if (!tournament.IsPublic)
            {
                tournament.JoinCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            }

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<Tournament?> GetTournamentByIdAsync(int id)
        {
            return await _context.Tournaments.FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
