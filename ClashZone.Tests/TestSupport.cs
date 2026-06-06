using System.Collections.Generic;
using System.Security.Claims;
using ClashZone.DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace ClashZone.Tests
{
    /// <summary>
    /// Wspólne narzędzia dla testów: fabryki mocków Identity oraz konfiguracja
    /// kontekstu kontrolera (zalogowany użytkownik, TempData).
    /// </summary>
    internal static class TestSupport
    {
        public static Mock<UserManager<ClashUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ClashUser>>();
            var mgr = new Mock<UserManager<ClashUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            return mgr;
        }

        public static Mock<SignInManager<ClashUser>> MockSignInManager(UserManager<ClashUser> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ClashUser>>();
            return new Mock<SignInManager<ClashUser>>(
                userManager, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
        }

        /// <summary>
        /// Ustawia na kontrolerze kontekst HTTP z (opcjonalnie) zalogowanym
        /// użytkownikiem oraz działające TempData.
        /// </summary>
        public static void SetUser(Controller controller, string? userId, bool isAuthenticated = true, params string[] roles)
        {
            var claims = new List<Claim>();
            if (isAuthenticated && userId != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
                claims.Add(new Claim(ClaimTypes.Name, userId));
                foreach (var r in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, r));
                }
            }
            var identity = isAuthenticated
                ? new ClaimsIdentity(claims, "TestAuth")
                : new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }
    }
}
