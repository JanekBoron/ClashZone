using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using DataAccess;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.DataAccess.Repository
{
    /// <summary>
    /// Concrete implementation of <see cref="IMatchesRepository"/> that uses
    /// <see cref="ApplicationDbContext"/> to query match-related data.
    /// Encapsulating EF Core queries within this class isolates data
    /// access logic, making it easier to test and refactor without
    /// affecting higher layers of the application.
    /// </summary>
    public class MatchesRepository : IMatchesRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="MatchesRepository"/>
        /// class with the specified database context.
        /// </summary>
        /// <param name="context">Database context used to execute queries.</param>
        public MatchesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId)
        {
            return await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .OrderByDescending(m => m.PlayedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Match?> GetMatchByIdAsync(int matchId, int tournamentId)
        {
            return await _context.Matches
                .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId);
        }

        /// <inheritdoc />
        public async Task<List<PlayerMatchStat>> GetPlayerStatsForMatchAsync(int matchId)
        {
            return await _context.PlayerMatchStats
                .Where(p => p.MatchId == matchId)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Team?> GetTeamByIdAsync(int teamId)
        {
            return await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
        }
    }
}