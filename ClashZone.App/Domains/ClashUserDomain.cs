
using Microsoft.AspNetCore.Identity;

namespace ClashZone.App.Domains
{
    public class ClashUserDomain : IdentityUser
    {
        /// <summary>
        /// Friendly nickname shown in public rankings and on profiles.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Total number of points or ELO the player has earned in tournaments.
        /// This property is optional and can be removed if the ranking
        /// system is stored elsewhere.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Additional notes or awards for the user.  You can replace
        /// this with a collection navigation property to a separate
        /// Award entity if the award system becomes more complex.
        /// </summary>
        public string? Awards { get; set; }

        /// <summary>
        /// Optional path to the user's profile picture relative to the
        /// wwwroot folder.  When set, this image replaces the default
        /// user icon in the navigation bar and on the profile page.
        /// </summary>
        public string? ProfilePicturePath { get; set; }

        public bool IsBanned { get; set; } = false;
    }
}
