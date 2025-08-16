using ClashZone.DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model wrapping a player's per‑match statistics.  This
    /// information originates from the <see cref="DataAccess.Models.PlayerMatchStat"/>
    /// entity and includes the associated <see cref="ClashUser"/> so
    /// views can display the player's username, display name and
    /// profile picture alongside the numerical data.
    /// </summary>
    public class PlayerStatViewModel
    {
        /// <summary>
        /// The user whose statistics are represented.
        /// </summary>
        public ClashUser Player { get; set; } = null!;

        /// <summary>
        /// Number of kills recorded in the match.
        /// </summary>
        public int Kills { get; set; }

        /// <summary>
        /// Number of deaths recorded in the match.
        /// </summary>
        public int Deaths { get; set; }

        /// <summary>
        /// Number of assists recorded in the match.
        /// </summary>
        public int Assists { get; set; }

        /// <summary>
        /// Computed kill/death ratio.  Returns zero when no deaths
        /// occurred to avoid division by zero.
        /// </summary>
        public double KDRatio => Deaths == 0 ? Kills : (double)Kills / Deaths;
    }
}