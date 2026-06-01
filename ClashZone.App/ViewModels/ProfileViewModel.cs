
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
        public UserStatDomain? Statistics { get; set; }
        public UserSubscriptionDomain? Subscription { get; set; }
    }
}