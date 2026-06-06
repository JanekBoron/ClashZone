using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    /// <summary>
    /// Pojedynczy wiersz rankingu. Łączy zagregowane statystyki gracza z
    /// dodatkowymi informacjami zależnymi od gry (np. ulubiona postać w LoL).
    /// </summary>
    public class RankingRowViewModel
    {
        public UserStatDomain Stat { get; set; } = null!;

        /// <summary>
        /// Najczęściej grana postać (champion). Wypełniane tylko dla rankingu
        /// League of Legends.
        /// </summary>
        public string? FavoriteChampion { get; set; }
    }
}
