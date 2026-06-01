using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class TeamMemberDomain 
    {
        public int TeamId { get; set; }
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the associated <see cref="Team"/>.  This
        /// allows Entity Framework to automatically load the team when
        /// including <c>TeamMember.Team</c> in queries.
        /// </summary>
        public TeamDomain Team { get; set; } = default!;
    }
}
