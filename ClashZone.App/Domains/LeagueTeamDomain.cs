using System;

namespace ClashZone.App.Domains
{
    public class LeagueTeamDomain
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string CaptainId { get; set; } = string.Empty;
        public string JoinCode { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
