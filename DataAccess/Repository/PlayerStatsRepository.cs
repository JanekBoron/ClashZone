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

            // REQUIRED - inserting data
            await _context.SaveChangesAsync();
        }

        public async Task<UserStatDomain?> GetUserStatsAsync(string userId)
        {
            var userStat = await _context.UserStats
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            return _mapper.Map<UserStatDomain?>(userStat);
        }

        public async Task<List<UserStatDomain>> GetAllUserStatsAsync()
        {
            var stats = await _context.UserStats
                .Include(s => s.User)
                .ToListAsync();

            return _mapper.Map<List<UserStatDomain>>(stats);
        }

        public async Task AddUserStatsAsync(UserStatDomain statsDomain)
        {
            var stats = _mapper.Map<UserStat>(statsDomain);

            await _context.UserStats.AddAsync(stats);

            // REQUIRED - inserting data
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserStats(UserStatDomain statsDomain)
        {
            var existing = await _context.UserStats.FindAsync(statsDomain.UserId);
            if (existing == null) return;
            _mapper.Map(statsDomain, existing);
            await _context.SaveChangesAsync();
        }
    }
}
