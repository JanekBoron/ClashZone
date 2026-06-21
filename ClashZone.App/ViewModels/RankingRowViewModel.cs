using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class RankingRowViewModel
    {
        public UserStatDomain Stat { get; set; } = null!;
        public string? FavoriteChampion { get; set; }
    }
}
