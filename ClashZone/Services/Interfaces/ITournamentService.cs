using ClashZone.DataAccess.Models;
using ClashZone.ViewModels;
using DataAccess.Models;

namespace ClashZone.Services.Interfaces
{
    public interface ITournamentService
    {
        Task<List<Tournament>> GetUpcomingTournamentsAsync(string? format);
        Task<List<Tournament>> GetUserTournamentsAsync(string userId);
        Task CreateTournamentAsync(Tournament tournament, string createdByUserId);
        Task<TournamentDetailsViewModel?> GetTournamentDetailsAsync(int id, string? userId);
        Task<JoinTournamentResult> JoinTournamentAsync(int id, string userId);

        Task<int?> JoinTeamAsync(int teamId, string userId, string code);
    }
}
