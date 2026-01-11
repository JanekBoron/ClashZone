using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public enum CoinWalletTransactionType
    {
        Earn = 1,       // przyznane np. za wygrane turnieje
        Spend = 2       // zakupy w sklepie
    }

    public class CoinWalletTransaction
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }
        [Required]
        [ForeignKey(nameof(Wallet))]
        public string UserId { get; set; } = default!;
        public CoinWallet Wallet { get; set; } = default!;

        public CoinWalletTransactionType Type { get; set; }

        public int Amount { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        [MaxLength(256)]
        public string? Reference { get; set; }
    }
}
