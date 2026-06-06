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
        /// <summary>
        /// Turniej premium — dołączyć mogą tylko użytkownicy z aktywnym pakietem
        /// Premium/Ultra. Gating jest egzekwowany w <c>TournamentService.JoinTournamentAsync</c>.
        /// </summary>
        public bool IsPremium { get; set; }
        public string FirstPlacePrize { get; set; } = string.Empty;
        public string SecondPlacePrize { get; set; } = string.Empty;
        public string ThirdPlacePrize { get; set; } = string.Empty;
        /// <summary>
        /// Reguły turnieju, każda w osobnej linii. Puste = domyślne reguły gry.
        /// </summary>
        public string? Rules { get; set; }
    }
}
