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
        public int MatchId { get; set; }
        public string UserId { get; set; } = null!;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }

        public Match Match { get; set; } = null!;
        public ClashUser User { get; set; } = null!;

        /// <summary>
        /// Computed kill/death ratio for this match.  If deaths is zero the
        /// ratio is set to kills to avoid division by zero.  Rounded to two
        /// decimal places for display.
        /// </summary>
        public double KD => Deaths != 0 ? Math.Round((double)Kills / Deaths, 2) : Kills;
    }
}