using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class MatchInfo
    {
        public string Team1Name { get; set; }
        public string Team2Name { get; set; }

        public int? Team1Score { get; set; }
        public int? Team2Score { get; set; }
        public int? Round {  get; set; }

        public int? MatchNum { get; set; }

    }
}
