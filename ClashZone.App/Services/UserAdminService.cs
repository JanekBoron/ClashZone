using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services
{
    public class UserAdminService : IUserAdminService
    {
        private readonly IUserRepository _userRepository;

        public UserAdminService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserListItemVm>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var result = new List<UserListItemVm>(users.Count);
            foreach (var u in users)
            {
                var roles = await _userRepository.GetRolesAsync(u.Id);
                result.Add(new UserListItemVm
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    UserName = u.UserName ?? "",
                    Roles = roles.ToList(),
                    IsBanned = u.IsBanned
                });
            }
            return result;
        }

        public async Task<bool> AddToRoleAsync(string userId, string roleName)
            => await _userRepository.AddToRoleAsync(userId, roleName);

        public async Task<bool> RemoveFromRoleAsync(string userId, string roleName)
            => await _userRepository.RemoveFromRoleAsync(userId, roleName);

        public async Task<bool> BanAsync(string userId)
            => await _userRepository.BanAsync(userId);

        public async Task<bool> UnbanAsync(string userId)
            => await _userRepository.UnbanAsync(userId);
    }
}
