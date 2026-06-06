using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Represents a single chat message posted within a tournament.  Messages
    /// can be directed to the entire tournament (TeamId is null) or
    /// restricted to a specific team.
    /// </summary>
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the tournament to which this message belongs.
        /// Null for league messages (see <see cref="SeasonId"/>).
        /// </summary>
        public int? TournamentId { get; set; }

        /// <summary>
        /// Foreign key to the season to which this message belongs.
        /// Null for tournament messages (see <see cref="TournamentId"/>).
        /// </summary>
        public int? SeasonId { get; set; }

        /// <summary>
        /// Foreign key to the team this message is for.  If null, the
        /// message is visible to all participants of the tournament/season.
        /// For league messages this holds the <c>LeagueTeam</c> id.
        /// </summary>
        public int? TeamId { get; set; }

        /// <summary>
        /// Identifier of the user who sent the message.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Text content of the chat message.
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the message was sent (UTC).
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsReport { get; set; }
    }
}
