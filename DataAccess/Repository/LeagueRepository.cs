using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.DataAccess.Repository
{
    public class LeagueRepository : ILeagueRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public LeagueRepository(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }


        public async Task AddSeasonAsync(SeasonDomain season)
        {
            var entity = _mapper.Map<Season>(season);
            _db.Seasons.Add(entity);
            await _db.SaveChangesAsync();
            season.Id = entity.Id;
        }

        public async Task<SeasonDomain?> GetSeasonByIdAsync(int id)
        {
            var entity = await _db.Seasons.FirstOrDefaultAsync(s => s.Id == id);
            return _mapper.Map<SeasonDomain?>(entity);
        }

        public async Task<List<SeasonDomain>> GetSeasonsAsync()
        {
            var entities = await _db.Seasons.OrderByDescending(s => s.StartDate).ToListAsync();
            return _mapper.Map<List<SeasonDomain>>(entities);
        }

        public async Task UpdateSeasonAsync(SeasonDomain season)
        {
            var existing = await _db.Seasons.FindAsync(season.Id);
            if (existing == null) return;
            _mapper.Map(season, existing);
            await _db.SaveChangesAsync();
        }


        public async Task<LeagueRegistrationDomain> AddRegistrationAsync(LeagueRegistrationDomain registration)
        {
            var entity = _mapper.Map<LeagueRegistration>(registration);
            _db.LeagueRegistrations.Add(entity);
            await _db.SaveChangesAsync();
            return _mapper.Map<LeagueRegistrationDomain>(entity);
        }

        public async Task<List<LeagueRegistrationDomain>> GetRegistrationsForSeasonAsync(int seasonId, bool includeRemoved = false)
        {
            var query = _db.LeagueRegistrations
                .Include(r => r.LeagueTeam)
                .Where(r => r.SeasonId == seasonId);
            if (!includeRemoved)
            {
                query = query.Where(r => !r.IsRemoved);
            }
            var entities = await query.ToListAsync();
            return _mapper.Map<List<LeagueRegistrationDomain>>(entities);
        }

        public async Task<LeagueRegistrationDomain?> GetRegistrationAsync(int seasonId, int leagueTeamId)
        {
            var entity = await _db.LeagueRegistrations
                .Include(r => r.LeagueTeam)
                .FirstOrDefaultAsync(r => r.SeasonId == seasonId && r.LeagueTeamId == leagueTeamId);
            return _mapper.Map<LeagueRegistrationDomain?>(entity);
        }

        public async Task<LeagueRegistrationDomain?> GetRegistrationByIdAsync(int id)
        {
            var entity = await _db.LeagueRegistrations
                .Include(r => r.LeagueTeam)
                .FirstOrDefaultAsync(r => r.Id == id);
            return _mapper.Map<LeagueRegistrationDomain?>(entity);
        }

        public async Task SetRemovedAsync(int registrationId, bool removed)
        {
            var entity = await _db.LeagueRegistrations.FindAsync(registrationId);
            if (entity == null) return;
            entity.IsRemoved = removed;
            await _db.SaveChangesAsync();
        }


        public async Task AddMatchAsync(LeagueMatchDomain match)
        {
            var entity = _mapper.Map<LeagueMatch>(match);
            _db.LeagueMatches.Add(entity);
            await _db.SaveChangesAsync();
            match.Id = entity.Id;
        }

        public async Task<List<LeagueMatchDomain>> GetMatchesForSeasonAsync(int seasonId)
        {
            var entities = await _db.LeagueMatches
                .Where(m => m.SeasonId == seasonId)
                .OrderBy(m => m.IsPlayoff).ThenBy(m => m.Week).ThenBy(m => m.Round).ThenBy(m => m.Id)
                .ToListAsync();
            return _mapper.Map<List<LeagueMatchDomain>>(entities);
        }

        public async Task<LeagueMatchDomain?> GetMatchByIdAsync(int id)
        {
            var entity = await _db.LeagueMatches.FirstOrDefaultAsync(m => m.Id == id);
            return _mapper.Map<LeagueMatchDomain?>(entity);
        }

        public async Task UpdateMatchAsync(LeagueMatchDomain match)
        {
            var existing = await _db.LeagueMatches.FindAsync(match.Id);
            if (existing == null) return;
            _mapper.Map(match, existing);
            await _db.SaveChangesAsync();
        }


        public async Task AddChatMessageAsync(ChatMessageDomain message)
        {
            _db.ChatMessages.Add(_mapper.Map<ChatMessage>(message));
            await _db.SaveChangesAsync();
        }

        public async Task<List<ChatMessageDomain>> GetAllChatMessagesAsync(int seasonId)
        {
            var messages = await _db.ChatMessages
                .Where(m => m.SeasonId == seasonId && m.TeamId == null && !m.IsReport)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
            return _mapper.Map<List<ChatMessageDomain>>(messages);
        }

        public async Task<List<ChatMessageDomain>> GetTeamChatMessagesAsync(int seasonId, int teamId)
        {
            var messages = await _db.ChatMessages
                .Where(m => m.SeasonId == seasonId && m.TeamId == teamId && !m.IsReport)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
            return _mapper.Map<List<ChatMessageDomain>>(messages);
        }

        public async Task<List<ChatMessageDomain>> GetReportChatMessagesAsync(int seasonId, int? teamId, bool isAdmin)
        {
            IQueryable<ChatMessage> query = _db.ChatMessages
                .Where(m => m.SeasonId == seasonId && m.IsReport);
            if (!isAdmin)
            {
                if (teamId == null) return new List<ChatMessageDomain>();
                query = query.Where(m => m.TeamId == teamId);
            }
            var messages = await query.OrderBy(m => m.SentAt).ToListAsync();
            return _mapper.Map<List<ChatMessageDomain>>(messages);
        }


        public async Task<List<PlayerMatchStatDomain>> GetPlayerStatsForLeagueMatchAsync(int leagueMatchId)
        {
            var stats = await _db.PlayerMatchStats
                .Where(p => p.LeagueMatchId == leagueMatchId)
                .ToListAsync();
            return _mapper.Map<List<PlayerMatchStatDomain>>(stats);
        }
    }
}
