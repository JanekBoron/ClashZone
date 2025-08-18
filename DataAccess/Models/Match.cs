using System;
using System.Collections.Generic;
using ClashZone.DataAccess.Models;

namespace DataAccess.Models
{
    /// <summary>
    /// Represents a completed match within a tournament.  A match links
    /// two teams together with scores for each side and the date/time
    /// the match was played.  Associated player statistics are stored
    /// in the <see cref="PlayerMatchStat"/> entity.
    /// </summary>
    public class Match
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the tournament this match belongs to.
        /// </summary>
        public int TournamentId { get; set; }

        public int? Team1Id { get; set; }

        public int? Team2Id { get; set; }

        /// <summary>
        /// Final score for Team1.
        /// </summary>
        public int Team1Score { get; set; }

        /// <summary>
        /// Final score for Team2.
        /// </summary>
        public int Team2Score { get; set; }

        /// <summary>
        /// Timestamp when the match was played. Defaults to UTC now.
        /// </summary>
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

        public Tournament Tournament { get; set; } = null!;
        public Team? Team1 { get; set; }
        public Team? Team2 { get; set; }

        /// <summary>
        /// Collection of per player statistics associated with this match.
        /// </summary>
        public List<ClashZone.DataAccess.Models.PlayerMatchStat> PlayerStats { get; set; } = new();
    }
}