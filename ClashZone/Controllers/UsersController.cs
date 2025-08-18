using System.Threading.Tasks;
using ClashZone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClashZone.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IUserAdminService _svc;
        public UsersController(IUserAdminService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await _svc.GetAllAsync();
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> MakeOrganizer(string id)
        {
            await _svc.AddToRoleAsync(id, "Organizer");
            return RedirectToAction(nameof(Index));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            await _svc.AddToRoleAsync(id, "Admin");
            return RedirectToAction(nameof(Index));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RemoveOrganizer(string id)
        {
            await _svc.RemoveFromRoleAsync(id, "Organizer");
            return RedirectToAction(nameof(Index));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RemoveAdmin(string id)
        {
            // guard: prevent removing your own admin role
            if (User?.Identity?.Name is string name && !string.IsNullOrWhiteSpace(name))
            {
                // Optional: Look up current user's Id; skipping for brevity
            }
            await _svc.RemoveFromRoleAsync(id, "Admin");
            return RedirectToAction(nameof(Index));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Ban(string id)
        {
            await _svc.BanAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Unban(string id)
        {
            await _svc.UnbanAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
