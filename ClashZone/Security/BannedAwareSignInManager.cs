using System.Threading.Tasks;
using ClashZone.DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClashZone.Security
{
    /// <summary>
    /// Blocks sign-in for users marked as IsBanned.
    /// </summary>
    public class BannedAwareSignInManager : SignInManager<ClashUser>
    {
        public BannedAwareSignInManager(
            UserManager<ClashUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ClashUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ClashUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ClashUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }
        public override Task<bool> CanSignInAsync(ClashUser user)
        {
            if (user?.IsBanned == true)
                return Task.FromResult(false);

            return base.CanSignInAsync(user);
        }
    }
}
