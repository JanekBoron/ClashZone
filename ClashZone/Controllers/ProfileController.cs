using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.ViewModels;
using ClashZone.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClashZone.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ClashUser> _userManager;
        private readonly SignInManager<ClashUser> _signInManager;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPlayerStatsRepository _playerStatsRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ICoinWalletRepository _wallets;
        private readonly IProductRedeemRepository _redemptions;
        private readonly IMapper _mapper;

        public ProfileController(
            UserManager<ClashUser> userManager,
            SignInManager<ClashUser> signInManager,
            ISubscriptionRepository subscriptionRepository,
            IPlayerStatsRepository playerStatsRepository,
            IWebHostEnvironment environment,
            ICoinWalletRepository wallets,
            IProductRedeemRepository redemptions,
            IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _subscriptionRepository = subscriptionRepository;
            _playerStatsRepository = playerStatsRepository;
            _environment = environment;
            _wallets = wallets;
            _redemptions = redemptions;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var stats = await _playerStatsRepository.GetUserStatsAsync(user.Id);
            var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(user.Id);

            var model = new ProfileViewModel
            {
                User = _mapper.Map<ClashUserDomain>(user),
                Statistics = stats,
                Subscription = subscription
            };
            return View(model);
        }

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

            if (!string.Equals(user.UserName, model.UserName, StringComparison.Ordinal))
            {
                var setNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setNameResult.Succeeded)
                {
                    foreach (var err in setNameResult.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    return View(model);
                }
            }

            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var err in setEmailResult.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    return View(model);
                }
            }

            user.DisplayName = model.DisplayName;

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
                        ModelState.AddModelError(string.Empty, err.Description);
                    return View(model);
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index));
        }
    }
}
