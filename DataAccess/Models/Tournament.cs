using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace DataAccess.Models
{
    public class Tournament
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public int MaxParticipants { get; set; }
        public string Format { get; set; } = string.Empty;
        public string FirstPlacePrize { get; set; } = string.Empty;
        public string SecondPlacePrize { get; set; } = string.Empty;
        public string ThirdPlacePrize { get; set; } = string.Empty;
        public string? Rules { get; set; }
        public string? CreatedByUserId { get; set; }
        public bool IsPublic { get; set; } = true;
        public string? JoinCode { get; set; }
        public bool IsPremium { get; set; } = false;
        public DateTime? ReminderSentAt { get; set; }
        public DateTime? InitialMatchRemindersSentAt { get; set; }
    }
}
