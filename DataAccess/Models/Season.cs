using System;
using System.ComponentModel.DataAnnotations;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Reprezentuje pojedynczy sezon ligi. Sezon przechodzi przez fazy:
    /// rejestracja → sezon regularny (parowanie wg win-ratio) → play-off →
    /// zakończony. Kluczowe daty (otwarcie/zamknięcie rejestracji, start sezonu)
    /// są prezentowane na osi czasu w stylu ESEA.
    /// </summary>
    public class Season
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Tytuł gry sezonu (CS2 / League of Legends).</summary>
        public string Game { get; set; } = string.Empty;

        /// <summary>Format drużyny, np. "5v5".</summary>
        public string Format { get; set; } = string.Empty;

        public DateTime RegistrationOpenDate { get; set; }
        public DateTime RegistrationCloseDate { get; set; }

        /// <summary>Data startu sezonu — po niej generowane są mecze otwarcia.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Wymagana minimalna liczba graczy w rosterze (z opłaconym wpisowym).</summary>
        public int RosterSize { get; set; } = 5;

        /// <summary>Liczba miejsc w play-off (top drużyny wg win-ratio).</summary>
        public int PlayoffSpots { get; set; } = 4;

        public string FirstPlacePrize { get; set; } = string.Empty;
        public string SecondPlacePrize { get; set; } = string.Empty;
        public string ThirdPlacePrize { get; set; } = string.Empty;

        /// <summary>Domyślny termin meczów (np. "Środa 20:00 CEST") — używany w schedulingu.</summary>
        public string? DefaultMatchDay { get; set; }

        /// <summary>
        /// Reguły sezonu, każda w osobnej linii. Gdy puste, używane są domyślne
        /// reguły gry z <c>GameCatalog.DefaultRules</c>.
        /// </summary>
        public string? Rules { get; set; }

        /// <summary>Status sezonu (wartości <c>SeasonStatus</c>).</summary>
        public int Status { get; set; }

        public string? CreatedByUserId { get; set; }
    }
}
