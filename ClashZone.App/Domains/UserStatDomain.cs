using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class UserStatDomain
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the <see cref="ClashUser"/> this statistic belongs to.
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Tytuł gry, której dotyczą zagregowane statystyki.
        /// </summary>
        public string Game { get; set; } = string.Empty;

        /// <summary>
        /// Total number of kills recorded for this user across all matches.
        /// </summary>
        public int TotalKills { get; set; }

        /// <summary>
        /// Total number of deaths recorded for this user across all matches.
        /// </summary>
        public int TotalDeaths { get; set; }

        /// <summary>
        /// Total number of assists recorded for this user across all matches.
        /// </summary>
        public int TotalAssists { get; set; }

        /// <summary>
        /// Number of matches the player has participated in.
        /// </summary>
        public int MatchesPlayed { get; set; }

        /// <summary>
        /// Computed average kill/death ratio for the user.  If the user
        /// has zero deaths then the ratio defaults to the total kills.
        /// This property is not mapped to the database.
        /// </summary>
        public double AverageKD => TotalDeaths != 0 ? Math.Round((double)TotalKills / TotalDeaths, 2) : TotalKills;

        /// <summary>
        /// Średni wskaźnik KDA = (zabójstwa + asysty) / śmierci. Używany przede
        /// wszystkim w rankingu League of Legends. Przy zerze śmierci dzielnik
        /// wynosi 1, aby uniknąć dzielenia przez zero.
        /// </summary>
        public double AverageKDA => Math.Round((double)(TotalKills + TotalAssists) / (TotalDeaths != 0 ? TotalDeaths : 1), 2);

        public ClashUserDomain User { get; set; } = null!;
    }
}
