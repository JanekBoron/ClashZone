using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.Web.ViewModels.Admin;

namespace ClashZone.Services.Interfaces
{
    public interface IUserAdminService
    {
        Task<List<UserListItemVm>> GetAllAsync();
        Task<bool> AddToRoleAsync(string userId, string roleName);
        Task<bool> RemoveFromRoleAsync(string userId, string roleName);
        Task<bool> BanAsync(string userId);
        Task<bool> UnbanAsync(string userId);
    }
}
