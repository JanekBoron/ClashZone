

using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class JoinTournamentResult
    {
        public int TournamentId { get; set; }
        public bool NotFound { get; set; }
        public bool RequiresSubscription { get; set; }
        public bool AlreadyJoined { get; set; }
        public bool MaxParticipantsExceeded { get; set; }
        public TeamDomain? Team { get; set; }
        public string? TournamentFormat { get; set; }
        public bool IsPremium { get; set; }
    }
}