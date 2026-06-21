using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class UserSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(Plan))]
        public int PlanId { get; set; }
        public SubscriptionPlan Plan { get; set; } = null!;

        public DateTime PurchaseDate { get; set; }

        public DateTime ExpiryDate { get; set; }
    }
}
