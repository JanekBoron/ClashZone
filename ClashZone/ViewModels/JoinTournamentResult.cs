using ClashZone.DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// Represents the outcome of attempting to join a tournament.
    /// Contains information about errors such as not found, subscription requirements,
    /// already joined, or exceeding the maximum number of participants.  Also carries
    /// details about the created team and tournament format when a join succeeds.
    /// </summary>
    public class JoinTournamentResult
    {
        /// <summary>
        /// The identifier of the tournament that was attempted to join.
        /// </summary>
        public int TournamentId { get; set; }

        /// <summary>
        /// Indicates that the tournament could not be found.
        /// </summary>
        public bool NotFound { get; set; }

        /// <summary>
        /// Indicates that a subscription is required to join.
        /// </summary>
        public bool RequiresSubscription { get; set; }

        /// <summary>
        /// Indicates that the current user has already joined the tournament.
        /// </summary>
        public bool AlreadyJoined { get; set; }

        /// <summary>
        /// Indicates that the maximum number of participants has been reached.
        /// </summary>
        public bool MaxParticipantsExceeded { get; set; }

        /// <summary>
        /// The created team if the join was successful (null otherwise).
        /// </summary>
        public Team? Team { get; set; }

        /// <summary>
        /// Format of the tournament (e.g. "1v1", "5v5") if available.
        /// </summary>
        public string? TournamentFormat { get; set; }

        /// <summary>
        /// Indicates whether the tournament is premium.
        /// </summary>
        public bool IsPremium { get; set; }
    }
}