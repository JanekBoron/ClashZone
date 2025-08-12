using ClashZone.ViewModels;

namespace ClashZone.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatViewModel?> GetChatAsync(int tournamentId, string? userId);
        Task PostMessageAsync(int tournamentId, string userId, string message, bool toTeam);
    }
}
