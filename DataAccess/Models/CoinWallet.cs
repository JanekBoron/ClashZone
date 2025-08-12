using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class CoinWallet
    {
        [Key] // klucz = to samo Id co użytkownik (AspNetUsers.Id)
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string UserId { get; set; } = default!;

        [Range(0, int.MaxValue)]
        public int Balance { get; set; } = 0;

        public ICollection<CoinWalletTransaction> Transactions { get; set; } = new List<CoinWalletTransaction>();
    }
}
