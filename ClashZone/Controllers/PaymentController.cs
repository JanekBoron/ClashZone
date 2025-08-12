using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System;
using System.Security.Claims;

namespace ClashZone.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly StripeSettings _stripeSettings;

        public PaymentController(ISubscriptionRepository repo, IOptions<StripeSettings> stripeSettings)
        {
            _subscriptionRepository = repo;
            _stripeSettings = stripeSettings.Value;
        }

        // Utwórz sesję Checkout i przekieruj użytkownika
        public async Task<IActionResult> CreateCheckoutSession(int planId)
        {
            var plan = (await _subscriptionRepository.GetAllPlansAsync()).First(p => p.Id == planId);
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions> {
          new SessionLineItemOptions {
            PriceData = new SessionLineItemPriceDataOptions {
              UnitAmount = (long)(plan.Price * 100), // Stripe przyjmuje kwoty w centach
              Currency = "eur",
              ProductData = new SessionLineItemPriceDataProductDataOptions {
                Name = $"{plan.Name} – miesięczna subskrypcja"
              }
            },
            Quantity = 1
          }
        },
                SuccessUrl = Url.Action("Success", "Payment", new { planId }, Request.Scheme),
                CancelUrl = Url.Action("Cancel", "Payment", null, Request.Scheme)
            };
            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return Redirect(session.Url);
        }

        // Po sukcesie płatności utwórz subskrypcję w bazie
        public async Task<IActionResult> Success(int planId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _subscriptionRepository.CreateSubscriptionAsync(userId, planId);
            return RedirectToAction("Index", "Subscription");
        }

        public IActionResult Cancel()
        {
            TempData["SubscriptionError"] = "Transakcja została anulowana.";
            return RedirectToAction("Index", "Subscription");
        }
    }
}

