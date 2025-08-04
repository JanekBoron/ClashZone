using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Definicja planu subskrypcyjnego.  Każdy plan ma unikalną nazwę,
    /// miesięczną cenę oraz określa czy umożliwia dostęp do turniejów premium
    /// i/lub ligi.  Plany mogą również określać okres ważności w dniach
    /// (domyślnie 30 dla miesięcznej subskrypcji).
    /// </summary>
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
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
