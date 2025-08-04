using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace DataAccess.Models
{
    /// <summary>
    /// Reprezentuje pojedynczy turniej w systemie Clash Zone.  Zawiera podstawowe
    /// informacje takie jak nazwa, tytuł gry, data rozpoczęcia czy liczba
    /// uczestników.  Pola te są używane w wielu miejscach w aplikacji przy
    /// tworzeniu i wyświetlaniu turniejów.
    /// </summary>
    public class Tournament
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public int MaxParticipants { get; set; }
        /// <summary>
        /// Format drużyny, np. "1v1", "2v2", "5v5".  Używany do określenia
        /// liczby graczy na drużynę.
        /// </summary>
        public string Format { get; set; } = string.Empty;
        public string Prize { get; set; } = string.Empty;
        public string? CreatedByUserId { get; set; }
        /// <summary>
        /// Określa czy turniej jest publiczny (dostępny bez zaproszenia).  Jeśli false,
        /// użytkownicy muszą podać unikalny kod dołączenia.
        /// </summary>
        public bool IsPublic { get; set; } = true;
        /// <summary>
        /// Kod dołączenia do prywatnego turnieju.  Jest generowany podczas tworzenia
        /// turnieju i może być udostępniany innym graczom.
        /// </summary>
        public string? JoinCode { get; set; }
        /// <summary>
        /// Określa czy turniej jest turniejem premium.  Użytkownicy bez aktywnej
        /// subskrypcji premium (lub pakietu Ultra) nie będą mogli dołączyć do
        /// tego turnieju.  Domyślnie false, aby istniejące turnieje pozostały
        /// dostępne dla wszystkich.
        /// </summary>
        public bool IsPremium { get; set; } = false;
    }
}
