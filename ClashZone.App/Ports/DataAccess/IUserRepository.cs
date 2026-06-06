using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface IUserRepository
    {
        Task<ClashUserDomain?> FindByIdAsync(string id);
        Task<List<ClashUserDomain>> GetAllAsync();
        Task<IList<string>> GetRolesAsync(string userId);
        Task<bool> AddToRoleAsync(string userId, string roleName);
        Task<bool> RemoveFromRoleAsync(string userId, string roleName);
        Task<bool> BanAsync(string userId);
        Task<bool> UnbanAsync(string userId);

        /// <summary>
        /// Zapisuje powiązanie konta Riot użytkownika (Riot ID + rozwiązany PUUID).
        /// </summary>
        Task<bool> SetRiotAccountAsync(string userId, string riotId, string puuid);
    }
}
