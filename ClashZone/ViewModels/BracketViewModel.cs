using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model representing a generated tournament bracket.
    /// It contains the tournament and a nested list of rounds,
    /// where each inner list represents the matches of a given round.
    /// </summary>
    public class BracketViewModel
    {
        public Tournament Tournament { get; set; } = null!;
        public List<List<MatchInfo>> Rounds { get; set; } = new();
    }
}
