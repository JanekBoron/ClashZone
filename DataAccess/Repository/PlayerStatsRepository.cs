using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Repository
{
    public class PlayerStatsRepository : IPlayerStatsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public PlayerStatsRepository(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task AddPlayerMatchStatAsync(PlayerMatchStatDomain playerStatDomain)
        {
            var playerStat = _mapper.Map<PlayerMatchStat>(playerStatDomain);

            await _context.PlayerMatchStats.AddAsync(playerStat);

            await _context.SaveChangesAsync();
        }

        public async Task<UserStatDomain?> GetUserStatsAsync(string userId, string game)
        {
            var userStat = await _context.UserStats
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Game == game);

            return _mapper.Map<UserStatDomain?>(userStat);
        }

        public async Task<List<UserStatDomain>> GetAllUserStatsAsync(string game)
        {
            var stats = await _context.UserStats
                .Include(s => s.User)
                .Where(s => s.Game == game)
                .ToListAsync();

            return _mapper.Map<List<UserStatDomain>>(stats);
        }

        public async Task<string?> GetMostPlayedChampionAsync(string userId, string game)
        {
            var champion = await _context.PlayerMatchStats
                .Where(p => p.UserId == userId && p.Game == game && p.Champion != null)
                .GroupBy(p => p.Champion!)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            return champion;
        }

        public async Task AddUserStatsAsync(UserStatDomain statsDomain)
        {
            var stats = _mapper.Map<UserStat>(statsDomain);

            await _context.UserStats.AddAsync(stats);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserStats(UserStatDomain statsDomain)
        {
            var existing = statsDomain.Id != 0
                ? await _context.UserStats.FindAsync(statsDomain.Id)
                : await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == statsDomain.UserId && s.Game == statsDomain.Game);
            if (existing == null) return;
            _mapper.Map(statsDomain, existing);
            await _context.SaveChangesAsync();
        }
    }
}
