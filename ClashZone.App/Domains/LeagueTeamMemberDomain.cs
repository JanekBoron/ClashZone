using System;

namespace ClashZone.App.Domains
{
    public class LeagueTeamMemberDomain
    {
        public int Id { get; set; }
        public int LeagueTeamId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
