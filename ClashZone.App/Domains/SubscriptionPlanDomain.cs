using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class SubscriptionPlanDomain
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsPremiumAccess { get; set; }
        public bool IsLeagueAccess { get; set; }
        public int DurationDays { get; set; } = 30;
    }
}
