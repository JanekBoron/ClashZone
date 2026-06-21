
namespace ClashZone.App.Domains
{
    public class CoinWalletDomain
    {
        public string UserId { get; set; } = default!;
        public int Balance { get; set; } = 0;
        public ICollection<CoinWalletTransactionDomain> Transactions { get; set; } = new List<CoinWalletTransactionDomain>();
    }
}
