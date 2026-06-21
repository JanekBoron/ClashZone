using ClashZone.DataAccess.Models;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace ClashZone.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ClashUser> _userManager;
        private readonly SignInManager<ClashUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ClashUser> userManager,
            SignInManager<ClashUser> signInManager,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userByName = await _userManager.FindByNameAsync(model.UserName);
            if (userByName != null)
            {
                ModelState.AddModelError(nameof(model.UserName), "Ta nazwa jest już zajęta");
                return View(model);
            }

            var userByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (userByEmail != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Ten email jest już zajęty");
                return View(model);
            }

            var user = new ClashUser
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var callbackUrl = Url.Action(
                    nameof(ConfirmEmail),
                    nameof(AccountController).Replace("Controller", string.Empty),
                    new { userId = user.Id, token },
                    Request.Scheme) ?? string.Empty;

                var message =
                    $"<p>Dziękujemy za rejestrację w ClashZone!</p>" +
                    $"<p>Aby aktywować konto, kliknij w link: " +
                    $"<a href=\"{callbackUrl}\">Aktywuj konto</a></p>";

                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email!,
                        "Aktywacja konta ClashZone",
                        message);

                    TempData["SuccessMessage"] =
                        "Na podany adres e-mail wysłano link aktywacyjny.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Nie udało się wysłać maila aktywacyjnego do {Email}.", user.Email);
                    TempData["SuccessMessage"] =
                        "Konto zostało utworzone, ale nie udało się wysłać maila aktywacyjnego. Spróbuj zresetować hasło lub skontaktuj się z obsługą.";
                }

                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email?.Trim());

            if (user != null)
            {
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "Konto nie jest potwierdzone. Sprawdź skrzynkę e-mail.");
                    return View(model);
                } else if (user != null && user.IsBanned)
                {
                    ModelState.AddModelError(string.Empty, "Twoje konto zostało zbanowane. Skontaktuj się z administratorem.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                        user.UserName,               
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false);

                if (result.Succeeded)
                    return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Nieprawidłowa próba logowania.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["SuccessMessage"] = "Jeśli adres e-mail istnieje w naszym systemie, wysłano wiadomość z instrukcją resetowania hasła.";
                return RedirectToAction(nameof(ForgotPassword));
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                nameof(AccountController).Replace("Controller", string.Empty),
                new { token, email = model.Email },
                Request.Scheme) ?? string.Empty;
            var message = $"<p>Otrzymaliśmy prośbę o zresetowanie hasła do Twojego konta ClashZone.</p><p>Aby ustawić nowe hasło, kliknij w link: <a href=\"{callbackUrl}\">Resetuj hasło</a></p>";
            try
            {
                await _emailService.SendEmailAsync(model.Email, "Resetowanie hasła w ClashZone", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Nie udało się wysłać maila resetującego hasło do {Email}.", model.Email);
            }
            TempData["SuccessMessage"] = "Jeśli adres e-mail istnieje w naszym systemie, wysłano wiadomość z instrukcją resetowania hasła.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return BadRequest();
            }
            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["SuccessMessage"] = "Twoje hasło zostało zresetowane.";
                return RedirectToAction(nameof(Login));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Hasło zostało zmienione. Możesz się teraz zalogować.";
                return RedirectToAction(nameof(Login));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest();
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Adres e-mail został potwierdzony. Możesz się teraz zalogować.";
            }
            else
            {
                TempData["ErrorMessage"] = "Nie udało się potwierdzić adresu e-mail.";
            }
            return RedirectToAction(nameof(Login));
        }
    }
}