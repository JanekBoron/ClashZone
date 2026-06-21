using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class BracketViewModel
    {
        public TournamentDomain Tournament { get; set; } = null!;
        public List<List<MatchInfoDomain>> Rounds { get; set; } = new();
    }
}
