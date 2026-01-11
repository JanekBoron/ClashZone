using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.ViewModels;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClashZone.Controllers
{
    /// <summary>
    /// Controller responsible for displaying and editing the currently logged in
    /// user's profile.  Users can view their basic information, active
    /// subscription, aggregated statistics and upload a custom avatar.  The
    /// edit form allows changing username, display name, email and password.
    /// </summary>
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ClashUser> _userManager;
        private readonly SignInManager<ClashUser> _signInManager;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ICoinWalletRepository _wallets;
        private readonly IProductRedeemRepository _redemptions;
        private readonly IProductsRepository _products;

        public ProfileController(
            UserManager<ClashUser> userManager,
            SignInManager<ClashUser> signInManager,
            ISubscriptionRepository subscriptionRepository,
            ApplicationDbContext context,
            IWebHostEnvironment environment, ICoinWalletRepository wallets, IProductRedeemRepository redemptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _subscriptionRepository = subscriptionRepository;
            _context = context;
            _environment = environment;
            _wallets = wallets;
            _redemptions = redemptions;
        }

        /// <summary>
        /// Displays the current user's profile including subscription details
        /// and aggregated statistics.  If no statistics exist a null value
        /// will be passed to the view.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var stats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == user.Id);
            var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);
            var model = new ProfileViewModel
            {
                User = user,
                Statistics = stats,
                Subscription = subscription
            };
            return View(model);
        }

        /// <summary>
        /// Displays the edit profile form prepopulated with the user's current
        /// information.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var model = new EditProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var order = await _wallets.GetPurchasedProductsAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return View(order);

        }

        /// <summary>
        /// Handles the edit profile submission.  Updates the user's basic
        /// information and optionally the password and profile picture.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Update user name if changed
            if (!string.Equals(user.UserName, model.UserName, StringComparison.Ordinal))
            {
                var setNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setNameResult.Succeeded)
                {
                    foreach (var err in setNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                    return View(model);
                }
            }
            // Update email if changed
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var err in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                    return View(model);
                }
            }
            // Update display name
            user.DisplayName = model.DisplayName;

            // Handle profile image upload
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsRoot = Path.Combine(_environment.WebRootPath ?? "wwwroot", "images", "profiles");
                if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);
                var ext = Path.GetExtension(model.ProfileImage.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }
                user.ProfilePicturePath = $"/images/profiles/{fileName}";
            }
            // Update password if provided
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError(string.Empty, "Aby zmienić hasło należy podać aktualne hasło.");
                    return View(model);
                }
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError(string.Empty, "Potwierdzenie hasła nie pasuje do nowego hasła.");
                    return View(model);
                }
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    foreach (var err in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                    return View(model);
                }
            }

            // Persist any other changes
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }
            // Refresh sign-in to update cookie after changes
            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index));
        }
    }
}