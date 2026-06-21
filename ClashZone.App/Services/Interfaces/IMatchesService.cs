using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services.Interfaces
{
    public interface IMatchesService
    {
        Task<List<MatchListItemViewModel>> GetMatchesForTournamentAsync(int tournamentId);
        Task<MatchDetailsViewModel?> GetMatchDetailsAsync(int tournamentId, int matchId);
    }
}