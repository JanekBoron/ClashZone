using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services
{





    /// <summary>
    /// ERROR HERE
    /// </summary>









    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionService(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<SubscriptionIndexViewModel> GetSubscriptionIndexViewModelAsync(string userId)
        {
            var plans = await _subscriptionRepository.GetAllPlansAsync();
            var active = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
            return new SubscriptionIndexViewModel
            {
                Plans = plans.ToList(),
                //ActiveSubscription = active
            };
        }

        public async Task PurchaseSubscriptionAsync(int planId, string userId)
        {
            await _subscriptionRepository.CreateSubscriptionAsync(userId, planId);
        }
    }
}
