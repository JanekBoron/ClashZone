using System;

namespace ClashZone.App.Domains
{
    public class LeagueRegistrationDomain
    {
        public int Id { get; set; }
        public int SeasonId { get; set; }
        public int LeagueTeamId { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool FeePaid { get; set; }
        public bool IsRemoved { get; set; }
        public LeagueTeamDomain? LeagueTeam { get; set; }
    }
}
