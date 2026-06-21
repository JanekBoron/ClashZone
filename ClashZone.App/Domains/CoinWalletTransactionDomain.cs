using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public enum CoinWalletTransactionType
    {
        Earn = 1,       
        Spend = 2     
    }
    public class CoinWalletTransactionDomain
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; } = default!;
        public CoinWalletDomain Wallet { get; set; } = default!;
        public CoinWalletTransactionType Type { get; set; }
        public int Amount { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public string? Reference { get; set; }
    }
}
