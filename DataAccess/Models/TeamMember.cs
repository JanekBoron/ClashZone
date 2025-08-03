using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        /// <summary>
        /// Navigation property to the associated <see cref="Team"/>.
        /// This allows Entity Framework to automatically load the team
        /// when including TeamMember.Team in queries.
        /// </summary>
        [ForeignKey(nameof(TeamId))]
        public Team Team { get; set; } = default!;
    }
}
