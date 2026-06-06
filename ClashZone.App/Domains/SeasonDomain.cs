using System;

namespace ClashZone.App.Domains
{
    public class SeasonDomain
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public DateTime RegistrationOpenDate { get; set; }
        public DateTime RegistrationCloseDate { get; set; }
        public DateTime StartDate { get; set; }
        public int RosterSize { get; set; }
        public int PlayoffSpots { get; set; }
        public string FirstPlacePrize { get; set; } = string.Empty;
        public string SecondPlacePrize { get; set; } = string.Empty;
        public string ThirdPlacePrize { get; set; } = string.Empty;
        public string? DefaultMatchDay { get; set; }
        public string? Rules { get; set; }
        public int Status { get; set; }
        public string? CreatedByUserId { get; set; }

        public SeasonStatus StatusEnum => (SeasonStatus)Status;
    }
}
