using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class MatchInfoDomain
    {
        //nullowalny string glupio
        public string Team1Name { get; set; }
        public string Team2Name { get; set; }

        // Optional: could be used in future to track winner or score
        //unikac null?
        public int? Team1Score { get; set; }
        public int? Team2Score { get; set; }
        public int? Round { get; set; }

        public int? MatchNum { get; set; }
    }
}
