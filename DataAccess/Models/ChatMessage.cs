using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int? TournamentId { get; set; }

        public int? SeasonId { get; set; }

        public int? TeamId { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsReport { get; set; }
    }
}
