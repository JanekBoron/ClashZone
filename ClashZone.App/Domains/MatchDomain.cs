using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class MatchDomain
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int? Team1Id { get; set; }
        public int? Team2Id { get; set; }
        public int Team1Score { get; set; }
        public int Team2Score { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
        public TournamentDomain Tournament { get; set; } = null!;
        public TeamDomain? Team1 { get; set; }
        public TeamDomain? Team2 { get; set; }
        public List<PlayerMatchStatDomain> PlayerStats { get; set; } = new();
    }
}
