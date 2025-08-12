using ClashZone.DataAccess.Models;
using ClashZone.ViewModels;
using DataAccess.Models;

namespace ClashZone.Services.Interfaces
{
    /// <summary>
    /// Result returned from a join tournament operation.  This result
    /// allows the service to communicate various outcomes back to the
    /// controller without leaking data access or business logic details.
    /// </summary>
    public class JoinTournamentResult
    {
        /// <summary>
        /// True when the target tournament could not be found.
        /// </summary>
        public bool NotFound { get; set; }

        /// <summary>
        /// Indicates that the tournament requires an active premium
        /// subscription and the user does not have one.
        /// </summary>
        public bool RequiresSubscription { get; set; }

        /// <summary>
        /// Indicates that the user is already a member of a team for
        /// the given tournament.  When this flag is set the Team
        /// property will be null because a new team is not created.
        /// </summary>
        public bool AlreadyJoined { get; set; }

        /// <summary>
        /// Newly created team when the join request succeeds.
        /// </summary>
        public Team? Team { get; set; }

        /// <summary>
        /// Identifier of the tournament associated with the team.  For a
        /// successful join this will be the same as the requested
        /// tournamentId; for a join via invitation it may reference the
        /// tournament of the invited team.
        /// </summary>
        public int? TournamentId { get; set; }

        /// <summary>
        /// Format of the tournament (e.g. "1v1", "2v2").  This is populated on
        /// successful join to allow controllers to determine whether an invite
        /// link should be generated.
        /// </summary>
        public string? TournamentFormat { get; set; }

        /// <summary>
        /// Indicates whether the tournament is premium.  Provided for
        /// completeness in case controllers need to display premium specific
        /// information on success.
        /// </summary>
        public bool IsPremium { get; set; }
    }
    public interface ITournamentService
    {
        Task<List<Tournament>> GetUpcomingTournamentsAsync(string? format);
        Task<List<Tournament>> GetUserTournamentsAsync(string userId);
        Task CreateTournamentAsync(Tournament tournament, string createdByUserId);
        Task<TournamentDetailsViewModel?> GetTournamentDetailsAsync(int id, string? userId);
        Task<JoinTournamentResult> JoinTournamentAsync(int id, string userId);

        Task<int?> JoinTeamAsync(int teamId, string userId, string code);
    }
}
