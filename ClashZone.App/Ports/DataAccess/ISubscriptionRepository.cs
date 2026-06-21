using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface ISubscriptionRepository
    {
        Task<SubscriptionPlanDomain[]> GetAllPlansAsync();

        Task<UserSubscriptionDomain?> GetActiveSubscriptionAsync(string userId);

        Task CreateSubscriptionAsync(string userId, int planId);
    }
}
