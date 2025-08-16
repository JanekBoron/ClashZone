using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model used for displaying a user's profile.  Contains the
    /// <see cref="ClashUser"/> entity along with aggregated statistics
    /// and the user's active subscription plan, if any.
    /// </summary>
    public class ProfileViewModel
    {
        public ClashUser User { get; set; } = null!;
        public UserStat? Statistics { get; set; }
        public UserSubscription? Subscription { get; set; }
    }
}