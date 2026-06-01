using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class UserSubscriptionDomain
    {
        public int Id { get; set; }

        /// <summary>
        /// Identyfikator użytkownika (klucz obcy do AspNetUsers).
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Klucz obcy do planu subskrypcyjnego.
        /// </summary>
        public int PlanId { get; set; }
        public SubscriptionPlanDomain Plan { get; set; } = null!;

        /// <summary>
        /// Data zakupu subskrypcji.
        /// </summary>
        public DateTime PurchaseDate { get; set; }

        /// <summary>
        /// Data wygaśnięcia subskrypcji (PurchaseDate + DurationDays planu).
        /// </summary>
        public DateTime ExpiryDate { get; set; }
    }
}
