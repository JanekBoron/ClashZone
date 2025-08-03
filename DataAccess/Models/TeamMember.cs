using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class TeamMember
    {
        [Key]
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}
