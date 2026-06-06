
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    /// <summary>
    /// View model used for displaying a user's profile.  Contains the
    /// <see cref="ClashUser"/> entity along with aggregated statistics
    /// and the user's active subscription plan, if any.
    /// </summary>
    public class ProfileViewModel
    {
        public ClashUserDomain User { get; set; } = null!;

        /// <summary>
        /// Zagregowane statystyki gracza dla Counter-Strike 2.
        /// </summary>
        public UserStatDomain? Cs2Statistics { get; set; }

        /// <summary>
        /// Zagregowane statystyki gracza dla League of Legends.
        /// </summary>
        public UserStatDomain? LolStatistics { get; set; }

        /// <summary>
        /// Najczęściej grana postać w League of Legends (jeśli dostępna).
        /// </summary>
        public string? LolFavoriteChampion { get; set; }

        public UserSubscriptionDomain? Subscription { get; set; }
    }
}