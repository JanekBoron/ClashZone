using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class TeamDomain
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public string CaptainId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string JoinCode { get; set; } = string.Empty;

        /// <summary>
        /// Czy drużyna została potwierdzona przez kapitana. Tylko potwierdzone
        /// drużyny liczą się do limitu turnieju i wchodzą do drabinki.
        /// </summary>
        public bool IsConfirmed { get; set; } = false;
    }
}
