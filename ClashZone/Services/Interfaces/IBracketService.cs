using ClashZone.ViewModels;

namespace ClashZone.Services.Interfaces
{
    public interface IBracketService
    {
        public Task<BracketViewModel?> GetBracketAsync(int tournamentId);

        /// <summary>
        /// Generates a bracket for the specified tournament with random scores and determines winners.
        /// This is used when the user requests to view results.
        /// </summary>
        public Task<BracketViewModel?> GetBracketWithResultsAsync(int tournamentId);
    }
}