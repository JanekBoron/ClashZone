using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
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
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public TournamentsRepository(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TournamentDomain>> GetUpcomingTournamentsAsync(string? format)
        {
            var query = _db.Tournaments.AsQueryable();
            query = query.Where(t => t.StartDate > DateTime.UtcNow);
            if (!string.IsNullOrEmpty(format))
            {
                query = query.Where(t => t.Format == format);
            }
            var tournaments = await query.OrderBy(t => t.StartDate).ToListAsync();
            return _mapper.Map<IEnumerable<TournamentDomain>>(tournaments);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TournamentDomain>> GetUserTournamentsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Array.Empty<TournamentDomain>();
            }
            // If you have a UserTournaments join table: return tournaments where the
            // user is a participant.  Otherwise fall back to tournaments created by
            // the user.
            var tournaments = await _db.Tournaments
                .Where(t => t.CreatedByUserId == userId)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
            return _mapper.Map<IEnumerable<TournamentDomain>>(tournaments);
        }

        /// <inheritdoc/>
        public async Task AddTournamentAsync(TournamentDomain tournament)
        {
            if (tournament == null)
                throw new ArgumentNullException(nameof(tournament));

            // If the tournament is private, generate a join code
            if (!tournament.IsPublic)
            {
                tournament.JoinCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            }
            var entity = _mapper.Map<Tournament>(tournament);
            _db.Tournaments.Add(entity);
            await _db.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<TournamentDomain?> GetTournamentByIdAsync(int id)
        {
            var tournament = await _db.Tournaments.FirstOrDefaultAsync(t => t.Id == id);
            return _mapper.Map<TournamentDomain?>(tournament);
        }

        /// <inheritdoc/>
        public async Task UpdateTournamentAsync(TournamentDomain tournament)
        {
            if (tournament == null) throw new ArgumentNullException(nameof(tournament));
            var existing = await _db.Tournaments.FindAsync(tournament.Id);
            if (existing == null) return;
            _mapper.Map(tournament, existing);
            await _db.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteTournamentAsync(int id)
        {
            var tournament = await _db.Tournaments.FindAsync(id);
            if (tournament != null)
            {
                _db.Tournaments.Remove(tournament);
                await _db.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<TeamDomain?> GetUserTeamAsync(int tournamentId, string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            // find membership including team relation
            var membership = await _db.TeamMembers
                .Include(tm => tm.Team)
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.Team.TournamentId == tournamentId);
            if (membership?.Team == null) return null;
            TeamDomain? teamDomain = _mapper.Map<TeamDomain?>(membership.Team);
            return teamDomain;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetTeamMemberIdsAsync(int teamId)
        {
            return await _db.TeamMembers
                .Where(tm => tm.TeamId == teamId)
                .Select(tm => tm.UserId)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<TeamMemberDomain>> GetTeamMembersAsync(int teamId)
        {
            var members = await _db.TeamMembers
                .Where(tm => tm.TeamId == teamId)
                .ToListAsync();
            return _mapper.Map<List<TeamMemberDomain>>(members);
        }

        /// <inheritdoc/>
        public async Task SetMemberReadyAsync(int teamId, string userId, bool ready)
        {
            var member = await _db.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
            if (member == null) return;
            member.IsReady = ready;
            await _db.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task ConfirmTeamAsync(int teamId)
        {
            var team = await _db.Teams.FindAsync(teamId);
            if (team == null) return;
            team.IsConfirmed = true;
            await _db.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<int> GetConfirmedTeamCountAsync(int tournamentId)
        {
            return await _db.Teams
                .CountAsync(t => t.TournamentId == tournamentId && t.IsConfirmed);
        }

        /// <inheritdoc/>
        public async Task<List<TeamDomain>> GetCaptainTeamsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<TeamDomain>();
            var teams = await _db.Teams
                .Where(t => t.CaptainId == userId)
                .ToListAsync();
            return _mapper.Map<List<TeamDomain>>(teams);
        }

        /// <inheritdoc/>
        public async Task<TeamDomain> CreateTeamWithCaptainAsync(int tournamentId, string userId)
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
            _db.Teams.Add(team);
            await _db.SaveChangesAsync();
            // Add captain as the first member
            var member = new TeamMember { TeamId = team.Id, UserId = userId };
            _db.TeamMembers.Add(member);
            await _db.SaveChangesAsync();
            return _mapper.Map<TeamDomain>(team);
        }

        /// <inheritdoc/>
        public async Task<int?> AddUserToTeamAsync(int teamId, string userId, string code)
        {
            var team = await _db.Teams.FindAsync(teamId);
            if (team == null || team.JoinCode != code) return null;
            // Check if user already a member
            var existing = await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId);
            if (existing == null)
            {
                _db.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = userId });
                await _db.SaveChangesAsync();
            }
            return team.TournamentId;
        }

        /// <inheritdoc/>
        public async Task AddChatMessageAsync(ChatMessageDomain message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _db.ChatMessages.Add(_mapper.Map<ChatMessage>(message));
            await _db.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ChatMessageDomain>> GetAllChatMessagesAsync(int tournamentId)
        {
            var messages = await _db.ChatMessages
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
            return _mapper.Map<List<ChatMessageDomain>>(messages);
        }

        /// <inheritdoc/>
        public async Task<List<ChatMessageDomain>> GetTeamChatMessagesAsync(int tournamentId, int teamId)
        {
            var messages = await _db.ChatMessages
                .Where(m =>
                    m.TournamentId == tournamentId &&
                    m.TeamId == teamId &&
                    m.IsReport == false)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
            return _mapper.Map<List<ChatMessageDomain>>(messages);
        }


        public async Task<List<ChatMessageDomain>> GetReportChatMessagesAsync(int tournamentId, int? teamId, bool isAdmin)
        {
            // Pobieramy wszystkie zgłoszenia dla turnieju
            IQueryable<ChatMessage> query = _db.ChatMessages
                .Where(cm => cm.TournamentId == tournamentId && cm.IsReport);

            // Jeśli użytkownik nie jest administratorem, ograniczamy wyniki do jego drużyny
            if (!isAdmin)
            {
                // Brak drużyny oznacza brak dostępu do zgłoszeń
                if (teamId == null)
                {  
                    return new List<ChatMessageDomain>();
                }

                query = query.Where(cm => cm.TeamId == teamId);
            }

            // Sortujemy zgłoszenia chronologicznie
            var chatMessages = await query.OrderBy(cm => cm.SentAt).ToListAsync();
            return _mapper.Map<List<ChatMessageDomain>>(chatMessages);
        }


        public async Task<List<TeamDomain>> GetTeamsForTournamentAsync(int tournamentId)
        {
            // Uczestnicy turnieju = wyłącznie drużyny potwierdzone przez kapitana.
            // Niepotwierdzone (niepełny skład / brak gotowości) nie liczą się do
            // limitu i nie wchodzą do drabinki.
            var teams = await _db.Teams
                .Where(t => t.TournamentId == tournamentId && t.IsConfirmed)
                .ToListAsync();
            return _mapper.Map<List<TeamDomain>>(teams);
        }

    }
}