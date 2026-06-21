
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class RulesViewModel
    {
        public TournamentDomain Tournament { get; set; } = null!;
        public List<string> Rules { get; set; } = new();
    }
}
