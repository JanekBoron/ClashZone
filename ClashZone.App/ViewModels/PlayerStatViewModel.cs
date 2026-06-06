
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
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
        public ClashUserDomain Player { get; set; } = null!;

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
        /// Postać (champion) zagrana przez gracza w meczu. Wypełniane tylko dla
        /// gier korzystających z postaci (League of Legends).
        /// </summary>
        public string? Champion { get; set; }

        /// <summary>
        /// Computed kill/death ratio.  Returns zero when no deaths
        /// occurred to avoid division by zero.
        /// </summary>
        public double KDRatio => Deaths == 0 ? Kills : (double)Kills / Deaths;

        /// <summary>
        /// Wskaźnik KDA = (zabójstwa + asysty) / śmierci. Używany dla League of
        /// Legends. Przy zerze śmierci dzielnik wynosi 1.
        /// </summary>
        public double KDA => (double)(Kills + Assists) / (Deaths == 0 ? 1 : Deaths);
    }
}