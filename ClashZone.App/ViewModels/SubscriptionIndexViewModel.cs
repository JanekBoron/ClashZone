
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class SubscriptionIndexViewModel
    {
        public List<SubscriptionPlanDomain> Plans { get; set; } = new List<SubscriptionPlanDomain>();
        public UserSubscriptionDomain? ActiveSubscription { get; set; }
    }
}
