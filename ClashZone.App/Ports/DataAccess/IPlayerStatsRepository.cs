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

        /// <summary>
        /// Pobiera zagregowane statystyki gracza dla wskazanej gry.
        /// </summary>
        Task<UserStatDomain?> GetUserStatsAsync(string userId, string game);

        /// <summary>
        /// Pobiera zagregowane statystyki wszystkich graczy dla wskazanej gry
        /// (na potrzeby rankingu).
        /// </summary>
        Task<List<UserStatDomain>> GetAllUserStatsAsync(string game);

        /// <summary>
        /// Zwraca najczęściej graną przez gracza postać (champion) w danej grze,
        /// lub null jeśli brak danych.
        /// </summary>
        Task<string?> GetMostPlayedChampionAsync(string userId, string game);

        Task AddUserStatsAsync(UserStatDomain stats);
        Task UpdateUserStats(UserStatDomain stats);
    }
}
