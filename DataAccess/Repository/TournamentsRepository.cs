using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using DataAccess;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /// <inheritdoc/>
        public async Task<Team?> GetUserTeamAsync(int tournamentId, string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            // find membership including team relation
            var membership = await _context.TeamMembers
                .Include(tm => tm.Team)
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.Team.TournamentId == tournamentId);
            return membership?.Team;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetTeamMemberIdsAsync(int teamId)
        {
            return await _context.TeamMembers
                .Where(tm => tm.TeamId == teamId)
                .Select(tm => tm.UserId)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Team> CreateTeamWithCaptainAsync(int tournamentId, string userId)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            // Create new team and generate join code
            var team = new Team
            {
                TournamentId = tournamentId,
                CaptainId = userId,
                JoinCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                Name = null
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            // Add captain as the first member
            var member = new TeamMember { TeamId = team.Id, UserId = userId };
            _context.TeamMembers.Add(member);
            await _context.SaveChangesAsync();
            return team;
        }

        /// <inheritdoc/>
        public async Task<int?> AddUserToTeamAsync(int teamId, string userId, string code)
        {
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null || team.JoinCode != code) return null;
            // Check if user already a member
            var existing = await _context.TeamMembers.FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId);
            if (existing == null)
            {
                _context.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = userId });
                await _context.SaveChangesAsync();
            }
            return team.TournamentId;
        }

        /// <inheritdoc/>
        public async Task AddChatMessageAsync(ChatMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ChatMessage>> GetAllChatMessagesAsync(int tournamentId)
        {
            return await _context.ChatMessages
                .Where(m => m.TournamentId == tournamentId && m.TeamId == null)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ChatMessage>> GetTeamChatMessagesAsync(int tournamentId, int teamId)
        {
            return await _context.ChatMessages
                .Where(m => m.TournamentId == tournamentId && m.TeamId == teamId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
    }
}
