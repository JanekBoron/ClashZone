using System;

namespace ClashZone.App.ViewModels
{
    public class TournamentFormViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public int MaxParticipants { get; set; }
        public string Format { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
        public string FirstPlacePrize { get; set; } = string.Empty;
        public string SecondPlacePrize { get; set; } = string.Empty;
        public string ThirdPlacePrize { get; set; } = string.Empty;
    }
}
