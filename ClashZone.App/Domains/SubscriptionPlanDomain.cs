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
        /// <summary>
        /// Czy plan daje dostęp do turniejów premium.
        /// </summary>
        public bool IsPremiumAccess { get; set; }
        /// <summary>
        /// Czy plan umożliwia dostęp do ligi.
        /// </summary>
        public bool IsLeagueAccess { get; set; }
        /// <summary>
        /// Długość subskrypcji w dniach.
        /// </summary>
        public int DurationDays { get; set; } = 30;
    }
}
