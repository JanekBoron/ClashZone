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
        public string UserId { get; set; } = string.Empty;
        public int PlanId { get; set; }
        public SubscriptionPlanDomain Plan { get; set; } = null!;
        public DateTime PurchaseDate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
