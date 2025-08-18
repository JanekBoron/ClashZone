using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClashZone.DataAccess.Models;
using ClashZone.Services.Interfaces;
using ClashZone.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.Services
{
    public class UserAdminService : IUserAdminService
    {
        private readonly UserManager<ClashUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserAdminService(UserManager<ClashUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<List<UserListItemVm>> GetAllAsync()
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var result = new List<UserListItemVm>(users.Count);
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
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
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return false;

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
            if (user is null) return false;
            var res = await _userManager.RemoveFromRoleAsync(user, roleName);
            return res.Succeeded;
        }

        public async Task<bool> BanAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return false;
            user.IsBanned = true;
            var res = await _userManager.UpdateAsync(user);
            return res.Succeeded;
        }

        public async Task<bool> UnbanAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return false;
            user.IsBanned = false;
            var res = await _userManager.UpdateAsync(user);
            return res.Succeeded;
        }
    }
}
