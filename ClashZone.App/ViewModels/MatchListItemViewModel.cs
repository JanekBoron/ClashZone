
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class MatchListItemViewModel
    {
        public MatchDomain Match { get; set; } = null!;
        public string GameTitle { get; set; } = string.Empty;
        public string Team1Name { get; set; } = string.Empty;
        public string Team2Name { get; set; } = string.Empty;
        public string Team1ProfileUrl { get; set; } = string.Empty;
        public string Team2ProfileUrl { get; set; } = string.Empty;
    }
}