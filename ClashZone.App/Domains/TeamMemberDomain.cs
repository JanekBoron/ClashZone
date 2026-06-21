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
        public bool IsReady { get; set; } = false;
        public TeamDomain Team { get; set; } = default!;
    }
}
