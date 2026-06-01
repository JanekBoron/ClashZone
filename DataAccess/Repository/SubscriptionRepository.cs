using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.DataAccess.Repository
{
    /// <summary>
    /// Implementacja repozytorium subskrypcji wykorzystująca Entity Framework.
    /// Udostępnia metody do pobierania planów, sprawdzania aktywnej subskrypcji
    /// oraz tworzenia nowej subskrypcji dla użytkownika.
    /// </summary>
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        public SubscriptionRepository(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<SubscriptionPlanDomain[]> GetAllPlansAsync()
        {
            var plans = await _db.SubscriptionPlans.ToListAsync();
            return _mapper.Map<SubscriptionPlanDomain[]>(plans);
        }

        public async Task<UserSubscriptionDomain?> GetActiveSubscriptionAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var subscription = await _db.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && s.ExpiryDate >= now)
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefaultAsync();
            return _mapper.Map<UserSubscriptionDomain?>(subscription);
        }
        public async Task CreateSubscriptionAsync(string userId, int planId)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(planId);
            if (plan == null)
            {
                return;
            }
            var purchaseDate = DateTime.UtcNow;
            var expiryDate = purchaseDate.AddDays(plan.DurationDays);
            var subscription = new UserSubscription
            {
                UserId = userId,
                PlanId = planId,
                PurchaseDate = purchaseDate,
                ExpiryDate = expiryDate
            };
            _db.UserSubscriptions.Add(subscription);
            await _db.SaveChangesAsync();
        }
      
    }
}
