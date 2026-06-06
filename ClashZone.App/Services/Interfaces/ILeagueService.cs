using ClashZone.App.Domains;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services.Interfaces
{
    public interface ILeagueService
    {
        // --- Przeglądanie / przegląd ---
        Task<List<SeasonDomain>> GetSeasonsAsync();
        Task<SeasonDetailsViewModel?> GetSeasonDetailsAsync(int id, string? userId);

        // --- Zarządzanie sezonem (admin/organizator) ---
        Task CreateSeasonAsync(SeasonFormViewModel model, string createdByUserId);
        Task UpdateSeasonAsync(SeasonFormViewModel model);
        Task<List<SeasonDomain>> GetManageListAsync();
        Task<SeasonFormViewModel?> GetSeasonFormAsync(int id);

        // --- Rejestracja drużyn ---
        Task<LeagueRegistrationResult> RegisterTeamAsync(int seasonId, int leagueTeamId, string userId);

        // --- Cykl rozgrywek ---
        Task CloseRegistrationAndPruneAsync(int seasonId);
        Task GenerateOpeningMatchupsAsync(int seasonId);
        Task GenerateNextWeekAsync(int seasonId);
        Task GenerateResultsAsync(int seasonId);
        Task GeneratePlayoffsAsync(int seasonId);

        // --- Widoki ligowe ---
        Task<List<LeagueStandingRow>> GetStandingsAsync(int seasonId);
        Task<LeagueMatchesViewModel?> GetMatchesAsync(int seasonId);
        Task<LeagueBracketViewModel?> GetPlayoffBracketAsync(int seasonId);
        Task<LeagueParticipantsViewModel?> GetParticipantsAsync(int seasonId);
        Task<LeagueRulesViewModel?> GetRulesAsync(int seasonId);

        // --- Play-off: symulacja meczu ---
        Task SimulatePlayoffMatchAsync(int seasonId, int matchId);

        /// <summary>Symuluje następny gotowy mecz play-off; zwraca false gdy brak.</summary>
        Task<bool> SimulateNextPlayoffMatchAsync(int seasonId);

        // --- Scheduling (podstawowy) ---
        Task<LeagueScheduleViewModel?> GetScheduleAsync(int matchId, string userId);
        Task ProposeScheduleAsync(int matchId, string userId, DateTime proposedTime, string? server);

        // --- Czat ---
        Task<LeagueChatViewModel?> GetChatAsync(int seasonId, string? userId, bool isAdmin);
        Task PostMessageAsync(int seasonId, string userId, string message, bool toTeam);
        Task PostReportMessageAsync(int seasonId, string userId, string message, int? teamId, bool isAdmin);

        // --- Szczegóły meczu ---
        Task<LeagueMatchDetailsViewModel?> GetMatchDetailsAsync(int seasonId, int matchId);
    }
}
