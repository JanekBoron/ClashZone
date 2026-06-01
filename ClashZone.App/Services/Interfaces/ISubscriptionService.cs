
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<SubscriptionIndexViewModel> GetSubscriptionIndexViewModelAsync(string userId);

        Task PurchaseSubscriptionAsync(int planId, string userId);
    }
}
