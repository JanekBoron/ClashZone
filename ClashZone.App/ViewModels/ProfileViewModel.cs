
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class ProfileViewModel
    {
        public ClashUserDomain User { get; set; } = null!;
        public UserStatDomain? Cs2Statistics { get; set; }
        public UserStatDomain? LolStatistics { get; set; }
        public string? LolFavoriteChampion { get; set; }
        public UserSubscriptionDomain? Subscription { get; set; }
    }
}