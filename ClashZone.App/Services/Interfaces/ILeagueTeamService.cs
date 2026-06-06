using ClashZone.App.Domains;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services.Interfaces
{
    public interface ILeagueTeamService
    {
        Task<LeagueTeamDomain> CreateTeamAsync(string name, string game, string captainId);

        Task<TeamManagementViewModel> GetUserTeamsAsync(string userId);

        /// <summary>
        /// Szczegóły drużyny dla podanego użytkownika. <c>InviteLink</c> jest
        /// uzupełniany przez kontroler (potrzebny <c>Url</c>).
        /// </summary>
        Task<LeagueTeamDetailsViewModel?> GetTeamDetailsAsync(int teamId, string userId);

        Task<JoinLeagueTeamResult> JoinByCodeAsync(int teamId, string code, string userId);
    }
}
