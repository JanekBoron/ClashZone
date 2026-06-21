using System.Threading.Tasks;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services.Interfaces
{
    public interface IBracketService
    {
        Task<BracketViewModel?> GetBracketAsync(int tournamentId);
        Task<BracketViewModel?> GetBracketWithResultsAsync(int tournamentId);
        Task<BracketViewModel?> GetBracketWithStatsAsync(int tournamentId);
        Task<BracketViewModel?> SimulateMatchAsync(int tournamentId, int roundNumber, int matchNumber, bool isAdmin = false);
    }
}