using System;
using System.ComponentModel.DataAnnotations.Schema;
using ClashZone.DataAccess.Models;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Represents aggregated statistics for a user across all matches.
    /// This entity stores total kills, deaths and assists as well as
    /// the number of matches played.  A convenience property
    /// <see cref="AverageKD"/> returns the kill/death ratio rounded
    /// to two decimal places.  See <see cref="PlayerMatchStat"/> for
    /// individual match statistics.
    /// </summary>
    public class UserStat
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the <see cref="ClashUser"/> this statistic belongs to.
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Total number of kills recorded for this user across all matches.
        /// </summary>
        public int TotalKills { get; set; }

        /// <summary>
        /// Total number of deaths recorded for this user across all matches.
        /// </summary>
        public int TotalDeaths { get; set; }

        /// <summary>
        /// Total number of assists recorded for this user across all matches.
        /// </summary>
        public int TotalAssists { get; set; }

        /// <summary>
        /// Number of matches the player has participated in.
        /// </summary>
        public int MatchesPlayed { get; set; }

        /// <summary>
        /// Computed average kill/death ratio for the user.  If the user
        /// has zero deaths then the ratio defaults to the total kills.
        /// This property is not mapped to the database.
        /// </summary>
        [NotMapped]
        public double AverageKD => TotalDeaths != 0 ? Math.Round((double)TotalKills / TotalDeaths, 2) : TotalKills;

        public ClashUser User { get; set; } = null!;
    }
}