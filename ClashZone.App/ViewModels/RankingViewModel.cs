
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class RankingViewModel
    {
        public List<RankingRowViewModel> Rows { get; set; } = new();
        public string SelectedGame { get; set; } = string.Empty;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }
    }
}