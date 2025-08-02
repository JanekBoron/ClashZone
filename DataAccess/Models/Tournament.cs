using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace DataAccess.Models
{
    public class Tournament
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!; // league, knockout, etc.
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

    }
}
