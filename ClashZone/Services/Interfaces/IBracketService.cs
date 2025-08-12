using ClashZone.ViewModels;

namespace ClashZone.Services.Interfaces
{
    public interface IBracketService
    {
        public Task<BracketViewModel?> GetBracketAsync(int tournamentId);
    }
}
