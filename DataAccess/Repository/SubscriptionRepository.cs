using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Repository
{
    /// <summary>
    /// Implementacja repozytorium subskrypcji wykorzystująca Entity Framework.
    /// Udostępnia metody do pobierania planów, sprawdzania aktywnej subskrypcji
    /// oraz tworzenia nowej subskrypcji dla użytkownika.
    /// </summary>
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context;
        public SubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SubscriptionPlan[]> GetAllPlansAsync()
        {
            return await _context.SubscriptionPlans.ToArrayAsync();
        }

        public async Task<UserSubscription?> GetActiveSubscriptionAsync(string userId)
        {
            var now = DateTime.UtcNow;
            return await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && s.ExpiryDate >= now)
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefaultAsync();
        }

        public async Task CreateSubscriptionAsync(string userId, int planId)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(planId);
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
            _context.UserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }
    }
}
