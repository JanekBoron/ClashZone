using ClashZone.App.Domains;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services.Interfaces
{
    public interface ILeagueTeamService
    {
        Task<LeagueTeamDomain> CreateTeamAsync(string name, string game, string captainId);
        Task<TeamManagementViewModel> GetUserTeamsAsync(string userId);
        Task<LeagueTeamDetailsViewModel?> GetTeamDetailsAsync(int teamId, string userId);
        Task<JoinLeagueTeamResult> JoinByCodeAsync(int teamId, string code, string userId);
    }
}
