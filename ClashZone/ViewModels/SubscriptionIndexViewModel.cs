using ClashZone.DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// ViewModel wykorzystywany do wyświetlania listy planów subskrypcyjnych w
    /// widoku Index kontrolera Subscription.  Zawiera listę wszystkich dostępnych
    /// planów oraz informację o aktywnej subskrypcji bieżącego użytkownika.
    /// </summary>
    public class SubscriptionIndexViewModel
    {
        public List<SubscriptionPlan> Plans { get; set; } = new List<SubscriptionPlan>();
        public UserSubscription? ActiveSubscription { get; set; }
    }
}
