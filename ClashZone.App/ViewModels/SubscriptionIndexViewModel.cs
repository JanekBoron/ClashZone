
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    /// <summary>
    /// ViewModel wykorzystywany do wyświetlania listy planów subskrypcyjnych w
    /// widoku Index kontrolera Subscription.  Zawiera listę wszystkich dostępnych
    /// planów oraz informację o aktywnej subskrypcji bieżącego użytkownika.
    /// </summary>
    public class SubscriptionIndexViewModel
    {
        public List<SubscriptionPlanDomain> Plans { get; set; } = new List<SubscriptionPlanDomain>();
        public UserSubscriptionDomain? ActiveSubscription { get; set; }
    }
}
