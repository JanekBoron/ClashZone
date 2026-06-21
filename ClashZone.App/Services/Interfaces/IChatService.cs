using ClashZone.App.ViewModels;
namespace ClashZone.App.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatViewModel?> GetChatAsync(int tournamentId, string? userId, bool isAdmin);
        Task PostMessageAsync(int tournamentId, string userId, string message, bool toTeam);
        Task PostReportMessageAsync(int tournamentId, string userId, string message, int? teamId, bool isAdmin);
    }
}