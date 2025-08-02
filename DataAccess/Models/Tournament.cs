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
        public string Prize { get; set; } = string.Empty;
        public string? CreatedByUserId { get; set; }
    }
}
