using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public decimal Price { get; set; }
        public bool IsPremiumAccess { get; set; }
        public bool IsLeagueAccess { get; set; }
        public int DurationDays { get; set; } = 30;
    }
}
