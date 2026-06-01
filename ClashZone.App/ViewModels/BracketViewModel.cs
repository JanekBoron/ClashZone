using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    /// <summary>
    /// View model representing a generated tournament bracket.
    /// It contains the tournament and a nested list of rounds,
    /// where each inner list represents the matches of a given round.
    /// </summary>
    public class BracketViewModel
    {
        public TournamentDomain Tournament { get; set; } = null!;
        public List<List<MatchInfoDomain>> Rounds { get; set; } = new();
    }
}
