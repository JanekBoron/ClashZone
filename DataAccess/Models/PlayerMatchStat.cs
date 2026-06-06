using System;
using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Represents a player's statistics for a single match.  Each
    /// instance is linked to a <see cref="Match"/> and a <see cref="ClashUser"/>.
    /// Kills, deaths and assists are recorded along with a computed
    /// kill/death ratio.
    /// </summary>
    public class PlayerMatchStat
    {
        public int Id { get; set; }

        /// <summary>FK do meczu turniejowego. Null dla statystyk meczów ligowych.</summary>
        public int? MatchId { get; set; }

        /// <summary>FK do meczu ligowego (<see cref="LeagueMatch"/>). Null dla turniejów.</summary>
        public int? LeagueMatchId { get; set; }

        public string UserId { get; set; } = null!;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }

        /// <summary>
        /// Tytuł gry, której dotyczy statystyka (np. "Counter Strike 2",
        /// "League of Legends"). Denormalizowany z turnieju, aby ułatwić
        /// zapytania o statystyki i rankingi w rozbiciu na grę.
        /// </summary>
        public string Game { get; set; } = string.Empty;

        /// <summary>
        /// Postać (champion) wybrana przez gracza. Wypełniane tylko dla gier,
        /// w których gracze grają postaciami (League of Legends).
        /// </summary>
        public string? Champion { get; set; }

        public Match? Match { get; set; }
        public ClashUser User { get; set; } = null!;

        /// <summary>
        /// Computed kill/death ratio for this match.  If deaths is zero the
        /// ratio is set to kills to avoid division by zero.  Rounded to two
        /// decimal places for display.
        /// </summary>
        public double KD => Deaths != 0 ? Math.Round((double)Kills / Deaths, 2) : Kills;
    }
}