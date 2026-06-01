using ClashZone.App.Domains;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services.Interfaces
{
    public interface ITournamentService
    {
        Task<List<TournamentDomain>> GetUpcomingTournamentsAsync(string? format);
        Task<List<TournamentDomain>> GetUserTournamentsAsync(string userId);
        Task CreateTournamentAsync(TournamentDomain tournament, string createdByUserId);
        Task<TournamentDetailsViewModel?> GetTournamentDetailsAsync(int id, string? userId);
        Task<JoinTournamentResult> JoinTournamentAsync(int id, string userId);

        Task<int?> JoinTeamAsync(int teamId, string userId, string code);
    }
}
