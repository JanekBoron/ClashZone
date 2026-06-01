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
    /// Concrete implementation of <see cref="IMatchesRepository"/> that uses
    /// <see cref="ApplicationDbContext"/> to query match-related data.
    /// Encapsulating EF Core queries within this class isolates data
    /// access logic, making it easier to test and refactor without
    /// affecting higher layers of the application.
    /// </summary>
    public class MatchesRepository : IMatchesRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public MatchesRepository(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<MatchDomain>> GetMatchesByTournamentAsync(int tournamentId)
        {
            var matches = await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .OrderByDescending(m => m.PlayedAt)
                .ToListAsync();

            return _mapper.Map<List<MatchDomain>>(matches);
        }

        public async Task<MatchDomain?> GetMatchByIdAsync(int matchId, int tournamentId)
        {
            var match = await _context.Matches
                .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId);

            return _mapper.Map<MatchDomain?>(match);
        }

        public async Task<List<PlayerMatchStatDomain>> GetPlayerStatsForMatchAsync(int matchId)
        {
            var playerStatsForMatch = await _context.PlayerMatchStats
                .Where(p => p.MatchId == matchId)
                .ToListAsync();

            return _mapper.Map<List<PlayerMatchStatDomain>>(playerStatsForMatch);
        }

        public async Task<TeamDomain?> GetTeamByIdAsync(int teamId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId);

            return _mapper.Map<TeamDomain?>(team);
        }

        public async Task AddMatchAsync(MatchDomain matchEntity)
        {
            var matchModel = _mapper.Map<Match>(matchEntity);

            await _context.Matches.AddAsync(matchModel);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsTournamentAlreadyAwardedAsync(
            int tournamentId,
            List<List<MatchInfoDomain>> rounds)
        {
            return await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .CountAsync() > rounds.Sum(r => r.Count);
        }

        public async Task<List<MatchDomain>> GetMatchesByTournamentIdAsync(int tournamentId)
        {
            var matches = await _context.Matches
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .Where(m => m.TournamentId == tournamentId)
                .ToListAsync();

            return _mapper.Map<List<MatchDomain>>(matches);
        }

        public async Task<bool> TournamentHasMatchesAsync(int tournamentId)
        {
            return await _context.Matches
                .AnyAsync(m => m.TournamentId == tournamentId);
        }
    }
}