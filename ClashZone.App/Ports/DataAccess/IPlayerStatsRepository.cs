using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface IPlayerStatsRepository
    {
        Task AddPlayerMatchStatAsync(PlayerMatchStatDomain playerStat);
        Task<UserStatDomain?> GetUserStatsAsync(string userId, string game);
        Task<List<UserStatDomain>> GetAllUserStatsAsync(string game);
        Task<string?> GetMostPlayedChampionAsync(string userId, string game);
        Task AddUserStatsAsync(UserStatDomain stats);
        Task UpdateUserStats(UserStatDomain stats);
    }
}
