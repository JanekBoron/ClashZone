using ClashZone.DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model used by the rankings page to supply a paginated list of
    /// user statistics as well as pagination metadata.  The AverageKD
    /// property of UserStat is used to compute the ordering externally.
    /// </summary>
    public class RankingViewModel
    {
        /// <summary>
        /// Collection of user statistics to be displayed on the current page.
        /// </summary>
        public List<UserStat> UserStats { get; set; } = new();

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
    }
}