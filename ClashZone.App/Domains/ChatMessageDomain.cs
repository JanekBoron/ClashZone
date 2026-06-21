
namespace ClashZone.App.Domains
{
    public class ChatMessageDomain
    {
        public int Id { get; set; }
        public int? TournamentId { get; set; }
        public int? SeasonId { get; set; }
        public int? TeamId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsReport { get; set; }
    }
}
