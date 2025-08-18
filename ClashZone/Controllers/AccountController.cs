using ClashZone.DataAccess.Models;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClashZone.Controllers
{
    /// <summary>
    /// Controller responsible for handling all user account related actions
    /// including registration, login, logout, email confirmation and
    /// password reset workflows.  Emails are dispatched using
    /// <see cref="IEmailService"/> to provide links for account activation
    /// and password resets.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ClashUser> _userManager;
        private readonly SignInManager<ClashUser> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ClashUser> userManager,
            SignInManager<ClashUser> signInManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        /// <summary>
        /// Displays the registration page.
        /// </summary>
        [HttpGet]
        public IActionResult Register() => View();

        /// <summary>
        /// Handles the registration form submission.  Creates a new user and
        /// sends an email confirmation link.  The user must confirm their
        /// address before being able to log in.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ClashUser { UserName = model.UserName, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Generate email confirmation token and send activation link
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action(
                        nameof(ConfirmEmail),
                        nameof(AccountController).Replace("Controller", string.Empty),
                        new { userId = user.Id, token },
                        Request.Scheme) ?? string.Empty;
                    var message = $"<p>Dziękujemy za rejestrację w ClashZone!</p><p>Aby aktywować konto, kliknij w link: <a href=\"{callbackUrl}\">Aktywuj konto</a></p>";
                    await _emailService.SendEmailAsync(user.Email!, "Aktywacja konta ClashZone", message);
                    TempData["SuccessMessage"] = "Na podany adres e-mail wysłano link aktywacyjny.";
                    return RedirectToAction(nameof(Login));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        /// <summary>
        /// Displays the login form.
        /// </summary>
        [HttpGet]
        public IActionResult Login() => View();

        /// <summary>
        /// Handles the login form submission.  Users must have confirmed their
        /// email address before they can sign in.  Appropriate error messages
        /// are displayed for invalid credentials or unconfirmed accounts.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Szukamy użytkownika po adresie e-mail (logowanie po e-mailu)
            var user = await _userManager.FindByEmailAsync(model.Email?.Trim());

            if (user != null)
            {
                // Ensure the user has confirmed their email before allowing login
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "Konto nie jest potwierdzone. Sprawdź skrzynkę e-mail.");
                    return View(model);
                } else if (user != null && user.IsBanned)
                {
                    ModelState.AddModelError(string.Empty, "Twoje konto zostało zbanowane. Skontaktuj się z administratorem.");
                    return View(model);
                }

                // Uwaga: PasswordSignInAsync wymaga NAZWY UŻYTKOWNIKA, nie e-maila
                var result = await _signInManager.PasswordSignInAsync(
                        user.UserName,               // <- kluczowa zmiana (wcześniej było user.Email)
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false);

                if (result.Succeeded)
                    return RedirectToAction("Index", "Home");
            }

            // Celowo nie zdradzamy, czy e-mail istnieje — komunikat ogólny
            ModelState.AddModelError(string.Empty, "Nieprawidłowa próba logowania.");
            return View(model);
        }

        /// <summary>
        /// Logs the user out of the application.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Displays the forgot password form.  Users enter their registered
        /// email address to receive a password reset link.
        /// </summary>
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        /// <summary>
        /// Handles the forgot password submission.  Generates a password reset
        /// token and emails a link to the user if the account exists and is
        /// confirmed.  Regardless of outcome a generic success message is
        /// displayed to avoid revealing user information.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                // Hide whether the email exists or is confirmed
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
            await _emailService.SendEmailAsync(model.Email, "Resetowanie hasła w ClashZone", message);
            TempData["SuccessMessage"] = "Jeśli adres e-mail istnieje w naszym systemie, wysłano wiadomość z instrukcją resetowania hasła.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        /// <summary>
        /// Displays the reset password form.  The token and email are passed in
        /// via the query string and bound to the view model for submission.
        /// </summary>
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

        /// <summary>
        /// Handles the reset password submission.  Validates the token and
        /// updates the user's password.  Upon success the user is redirected
        /// to the login page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Do not reveal that the user does not exist
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

        /// <summary>
        /// Confirms a user's email address using the token generated at
        /// registration time.  Displays a success or failure message and
        /// redirects to the login page.
        /// </summary>
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