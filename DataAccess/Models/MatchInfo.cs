using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Represents a pairing of two teams within a bracket round.
    /// Either Team1Name or Team2Name may be null to indicate a bye.
    /// A bye means the non-null team advances automatically.
    /// </summary>
    public class MatchInfo
    {
        //nullowalny string glupio
        public string Team1Name { get; set; }
        public string Team2Name { get; set; }

        // Optional: could be used in future to track winner or score
        //unikac null?
        public int? Team1Score { get; set; }
        public int? Team2Score { get; set; }
        public int? Round {  get; set; }

        public int? MatchNum { get; set; }

    }
}
