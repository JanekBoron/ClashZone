using System;
using System.ComponentModel.DataAnnotations;

namespace ClashZone.DataAccess.Models
{
    public class LeagueTeam
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }

        public string CaptainId { get; set; } = string.Empty;

        public string JoinCode { get; set; } = string.Empty;

        public string Game { get; set; } = string.Empty;

        public string? LogoPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
