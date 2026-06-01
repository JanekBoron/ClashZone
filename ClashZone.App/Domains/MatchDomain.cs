using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class MatchDomain
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

        public TournamentDomain Tournament { get; set; } = null!;
        public TeamDomain? Team1 { get; set; }
        public TeamDomain? Team2 { get; set; }

        /// <summary>
        /// Collection of per player statistics associated with this match.
        /// </summary>
        public List<PlayerMatchStatDomain> PlayerStats { get; set; } = new();
    }
}
