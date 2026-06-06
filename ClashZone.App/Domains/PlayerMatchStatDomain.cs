using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class PlayerMatchStatDomain
    {
        public int Id { get; set; }
        public int? MatchId { get; set; }
        public int? LeagueMatchId { get; set; }
        public string UserId { get; set; } = null!;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }

        /// <summary>
        /// Tytuł gry, której dotyczy statystyka.
        /// </summary>
        public string Game { get; set; } = string.Empty;

        /// <summary>
        /// Postać (champion) wybrana przez gracza (tylko League of Legends).
        /// </summary>
        public string? Champion { get; set; }

        public MatchDomain? Match { get; set; }
        public ClashUserDomain User { get; set; } = null!;

        /// <summary>
        /// Computed kill/death ratio for this match.  If deaths is zero the
        /// ratio is set to kills to avoid division by zero.  Rounded to two
        /// decimal places for display.
        /// </summary>
        public double KD => Deaths != 0 ? Math.Round((double)Kills / Deaths, 2) : Kills;
    }
}
