using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public string CaptainId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string JoinCode { get; set; } = string.Empty;
    }
}
