using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Reprezentuje subskrypcję wykupioną przez użytkownika na określony plan.
    /// Zawiera identyfikatory użytkownika i planu, datę zakupu oraz datę
    /// wygaśnięcia.  Dzięki temu możliwe jest sprawdzenie, czy subskrypcja jest
    /// jeszcze aktywna.
    /// </summary>
    public class UserSubscription
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Identyfikator użytkownika (klucz obcy do AspNetUsers).
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Klucz obcy do planu subskrypcyjnego.
        /// </summary>
        [ForeignKey(nameof(Plan))]
        public int PlanId { get; set; }
        public SubscriptionPlan Plan { get; set; } = null!;

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
