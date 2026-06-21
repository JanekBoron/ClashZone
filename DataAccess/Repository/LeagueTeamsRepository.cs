using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.DataAccess.Repository
{
    public class LeagueTeamsRepository : ILeagueTeamsRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public LeagueTeamsRepository(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<LeagueTeamDomain> CreateTeamAsync(LeagueTeamDomain team)
        {
            var entity = _mapper.Map<LeagueTeam>(team);
            _db.LeagueTeams.Add(entity);
            await _db.SaveChangesAsync();
            _db.LeagueTeamMembers.Add(new LeagueTeamMember
            {
                LeagueTeamId = entity.Id,
                UserId = entity.CaptainId,
                JoinedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return _mapper.Map<LeagueTeamDomain>(entity);
        }

        public async Task<LeagueTeamDomain?> GetByIdAsync(int id)
        {
            var entity = await _db.LeagueTeams.FirstOrDefaultAsync(t => t.Id == id);
            return _mapper.Map<LeagueTeamDomain?>(entity);
        }

        public async Task<List<LeagueTeamDomain>> GetUserTeamsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<LeagueTeamDomain>();
            var memberTeamIds = await _db.LeagueTeamMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.LeagueTeamId)
                .ToListAsync();
            var teams = await _db.LeagueTeams
                .Where(t => t.CaptainId == userId || memberTeamIds.Contains(t.Id))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return _mapper.Map<List<LeagueTeamDomain>>(teams);
        }

        public async Task<List<string>> GetMemberIdsAsync(int teamId)
        {
            return await _db.LeagueTeamMembers
                .Where(m => m.LeagueTeamId == teamId)
                .Select(m => m.UserId)
                .ToListAsync();
        }

        public async Task AddMemberAsync(int teamId, string userId)
        {
            _db.LeagueTeamMembers.Add(new LeagueTeamMember
            {
                LeagueTeamId = teamId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task<bool> IsMemberAsync(int teamId, string userId)
        {
            return await _db.LeagueTeamMembers.AnyAsync(m => m.LeagueTeamId == teamId && m.UserId == userId);
        }

        public async Task<int> CountMembersJoinedSinceAsync(int teamId, DateTime since)
        {
            var captainId = await _db.LeagueTeams
                .Where(t => t.Id == teamId)
                .Select(t => t.CaptainId)
                .FirstOrDefaultAsync();
            return await _db.LeagueTeamMembers
                .CountAsync(m => m.LeagueTeamId == teamId && m.JoinedAt >= since && m.UserId != captainId);
        }

        public async Task<bool> UserJoinedAnyTeamSinceAsync(string userId, DateTime since)
        {
            return await _db.LeagueTeamMembers
                .AnyAsync(m => m.UserId == userId && m.JoinedAt >= since);
        }
    }
}
