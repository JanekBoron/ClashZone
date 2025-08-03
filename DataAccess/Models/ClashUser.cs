using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ClashZone.DataAccess.Models
{
    public class ClashUser : IdentityUser
    {
        /// <summary>
        /// Friendly nickname shown in public rankings and on profiles.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Total number of points or ELO the player has earned in tournaments.  This
        /// property is optional and can be removed if the ranking system is stored
        /// elsewhere.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Additional notes or awards for the user.  You can replace this with a
        /// collection navigation property to a separate Award entity if the award
        /// system becomes more complex.
        /// </summary>
        public string? Awards { get; set; }
    }
}
