using System;

namespace ClashZone.App.Domains
{
    public class LeagueMatchDomain
    {
        public int Id { get; set; }
        public int SeasonId { get; set; }
        public int Week { get; set; }
        public int? HomeRegistrationId { get; set; }
        public int? AwayRegistrationId { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public bool IsPlayed { get; set; }
        public DateTime ScheduledTime { get; set; }
        public DateTime? ProposedTime { get; set; }
        public int? ProposedByRegistrationId { get; set; }
        public string? Server { get; set; }
        public bool IsPlayoff { get; set; }
        public int? Round { get; set; }
    }
}
