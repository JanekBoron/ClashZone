using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model representing tournament rules.  Contains the tournament
    /// context and a list of rules that apply to the tournament.  Rules
    /// may vary depending on the game associated with the tournament.
    /// </summary>
    public class RulesViewModel
    {
        /// <summary>
        /// Tournament for which rules are displayed.
        /// </summary>
        public Tournament Tournament { get; set; } = null!;

        /// <summary>
        /// Collection of rule descriptions.  The ordering of rules should
        /// reflect their importance or the logical order in which they
        /// should be read.
        /// </summary>
        public List<string> Rules { get; set; } = new();
    }
}
