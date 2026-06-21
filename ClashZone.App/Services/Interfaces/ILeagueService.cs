using ClashZone.App.Domains;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services.Interfaces
{
    public interface ILeagueService
    {
        Task<List<SeasonDomain>> GetSeasonsAsync();
        Task<SeasonDetailsViewModel?> GetSeasonDetailsAsync(int id, string? userId);

        Task CreateSeasonAsync(SeasonFormViewModel model, string createdByUserId);
        Task UpdateSeasonAsync(SeasonFormViewModel model);
        Task<List<SeasonDomain>> GetManageListAsync();
        Task<SeasonFormViewModel?> GetSeasonFormAsync(int id);

        Task<LeagueRegistrationResult> RegisterTeamAsync(int seasonId, int leagueTeamId, string userId);

        Task CloseRegistrationAndPruneAsync(int seasonId);
        Task GenerateOpeningMatchupsAsync(int seasonId);
        Task GenerateNextWeekAsync(int seasonId);
        Task GenerateResultsAsync(int seasonId);
        Task GeneratePlayoffsAsync(int seasonId);

        Task<List<LeagueStandingRow>> GetStandingsAsync(int seasonId);
        Task<LeagueMatchesViewModel?> GetMatchesAsync(int seasonId);
        Task<LeagueBracketViewModel?> GetPlayoffBracketAsync(int seasonId);
        Task<LeagueParticipantsViewModel?> GetParticipantsAsync(int seasonId);
        Task<LeagueRulesViewModel?> GetRulesAsync(int seasonId);

        Task SimulatePlayoffMatchAsync(int seasonId, int matchId);

        Task<bool> SimulateNextPlayoffMatchAsync(int seasonId);

        Task<LeagueScheduleViewModel?> GetScheduleAsync(int matchId, string userId);
        Task ProposeScheduleAsync(int matchId, string userId, DateTime proposedTime, string? server);

        Task<LeagueChatViewModel?> GetChatAsync(int seasonId, string? userId, bool isAdmin);
        Task PostMessageAsync(int seasonId, string userId, string message, bool toTeam);
        Task PostReportMessageAsync(int seasonId, string userId, string message, int? teamId, bool isAdmin);

        Task<LeagueMatchDetailsViewModel?> GetMatchDetailsAsync(int seasonId, int matchId);
    }
}
