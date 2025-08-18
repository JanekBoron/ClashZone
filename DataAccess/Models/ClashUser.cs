using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Represents an authenticated player within the ClashZone platform.
    /// Inherits from <see cref="IdentityUser"/> to provide basic identity
    /// properties such as UserName, Email and Id.  Additional fields
    /// specific to the gaming context such as DisplayName, Score and Awards
    /// are included here.  A new optional property <see cref="ProfilePicturePath"/>
    /// stores the relative path to the user's custom avatar image.  When
    /// set this image will replace the default user icon on the UI.
    /// </summary>
    public class ClashUser : IdentityUser
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

        [Required]
        public bool IsBanned { get; set; } = false;
    }
}