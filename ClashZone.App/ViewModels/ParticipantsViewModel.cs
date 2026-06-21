
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class ParticipantsViewModel
    {
        public TournamentDomain Tournament { get; set; } = null!;
        public List<ParticipantTeamViewModel> Teams { get; set; } = new();
    }
    public class ParticipantTeamViewModel
    {
        public int TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Members { get; set; } = new();
    }
}
