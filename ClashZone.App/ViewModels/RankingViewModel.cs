
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    /// <summary>
    /// View model used by the rankings page to supply a paginated list of
    /// user statistics as well as pagination metadata.  The AverageKD
    /// property of UserStat is used to compute the ordering externally.
    /// </summary>
    public class RankingViewModel
    {
        /// <summary>
        /// Wiersze rankingu wyświetlane na bieżącej stronie (statystyki +
        /// informacje zależne od gry).
        /// </summary>
        public List<RankingRowViewModel> Rows { get; set; } = new();

        /// <summary>
        /// Aktualnie wybrana gra (zakładka). Steruje zestawem kolumn.
        /// </summary>
        public string SelectedGame { get; set; } = string.Empty;

        /// <summary>
        /// The current page number (1‑indexed).
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Total number of pages available.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Number of items shown per page.  This value is determined by
        /// RankingsController and should remain constant across pages.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Aktualna fraza wyszukiwania (filtr po nazwie gracza). Pusta lub null
        /// oznacza brak filtra. Przenoszona między stronami i zakładkami gier.
        /// </summary>
        public string? SearchTerm { get; set; }
    }
}