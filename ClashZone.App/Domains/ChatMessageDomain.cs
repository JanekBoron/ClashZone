
namespace ClashZone.App.Domains
{
    public class ChatMessageDomain
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the tournament to which this message belongs.
        /// Null for league messages (see <see cref="SeasonId"/>).
        /// </summary>
        public int? TournamentId { get; set; }

        /// <summary>
        /// Foreign key to the season to which this message belongs.
        /// Null for tournament messages.
        /// </summary>
        public int? SeasonId { get; set; }

        /// <summary>
        /// Foreign key to the team this message is for.  If null, the
        /// message is visible to all participants of the tournament/season.
        /// </summary>
        public int? TeamId { get; set; }

        /// <summary>
        /// Identifier of the user who sent the message.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Text content of the chat message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the message was sent (UTC).
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsReport { get; set; }
    }
}
