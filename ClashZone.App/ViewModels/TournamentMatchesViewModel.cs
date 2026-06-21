using ClashZone.App.Domains;
using System.Collections.Generic;

namespace ClashZone.App.ViewModels
{
    public class TournamentMatchesViewModel
    {
        public TournamentDomain Tournament { get; set; } = null!;
        public List<MatchListItemViewModel> Matches { get; set; } = new();
    }
}