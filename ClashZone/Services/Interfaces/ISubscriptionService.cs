using ClashZone.ViewModels;

namespace ClashZone.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<SubscriptionIndexViewModel> GetSubscriptionIndexViewModelAsync(string userId);

        Task PurchaseSubscriptionAsync(int planId, string userId);
    }
}
