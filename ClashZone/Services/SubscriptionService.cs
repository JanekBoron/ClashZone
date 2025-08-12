using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using Stripe;

namespace ClashZone.Services
{
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
                ActiveSubscription = active
            };
        }

        public async Task PurchaseSubscriptionAsync(int planId, string userId)
        {
            await _subscriptionRepository.CreateSubscriptionAsync(userId, planId);
        }
    }
}
