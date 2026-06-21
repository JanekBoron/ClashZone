
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class ChatViewModel
    {
        public TournamentDomain Tournament { get; set; } = default!;
        public List<ChatMessageDomain> AllMessages { get; set; } = new List<ChatMessageDomain>();
        public List<ChatMessageDomain> TeamMessages { get; set; } = new List<ChatMessageDomain>();
        public List<ChatMessageDomain> ReportMessages { get; set; } = new List<ChatMessageDomain>();
        public bool HasTeam { get; set; }
        public int? UserTeamId { get; set; }
        public Dictionary<string, string> UserNames { get; set; } = new Dictionary<string, string>();
    }
}
