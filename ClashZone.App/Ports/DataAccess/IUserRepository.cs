using ClashZone.App.Domains;
using Microsoft.AspNetCore.Identity;

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
    }
}
