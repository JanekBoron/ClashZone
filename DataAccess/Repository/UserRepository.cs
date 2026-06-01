using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.DataAccess.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ClashUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;

        public UserRepository(UserManager<ClashUser> userManager, RoleManager<IdentityRole> roleManager, IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }

        public async Task<ClashUserDomain?> FindByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            return _mapper.Map<ClashUserDomain?>(user);
        }

        public async Task<List<ClashUserDomain>> GetAllAsync()
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            return _mapper.Map<List<ClashUserDomain>>(users);
        }

        public async Task<IList<string>> GetRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> AddToRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var create = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (!create.Succeeded) return false;
            }
            var res = await _userManager.AddToRoleAsync(user, roleName);
            return res.Succeeded;
        }

        public async Task<bool> RemoveFromRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            var res = await _userManager.RemoveFromRoleAsync(user, roleName);
            return res.Succeeded;
        }

        public async Task<bool> BanAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            user.IsBanned = true;
            var res = await _userManager.UpdateAsync(user);
            return res.Succeeded;
        }

        public async Task<bool> UnbanAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            user.IsBanned = false;
            var res = await _userManager.UpdateAsync(user);
            return res.Succeeded;
        }
    }
}
