using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class TournamentDomain
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public int MaxParticipants { get; set; }
        public string Format { get; set; } = string.Empty;
        public string FirstPlacePrize { get; set; } = string.Empty;
        public string SecondPlacePrize { get; set; } = string.Empty;
        public string ThirdPlacePrize { get; set; } = string.Empty;
        public string? Rules { get; set; }
        public string? CreatedByUserId { get; set; }
        public bool IsPublic { get; set; } = true;
        public string? JoinCode { get; set; }
        public bool IsPremium { get; set; } = false;
    }
}
