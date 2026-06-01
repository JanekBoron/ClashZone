using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface ICoinWalletRepository
    {
        Task<CoinWalletDomain> GetOrCreateAsync(string userId);
        Task AddTransactionAsync(CoinWalletTransactionDomain tx);
        Task<int> GetBalanceAsync(string userId);
        Task SaveChangesAsync();
        Task <List<ProductDomain>> GetPurchasedProductsAsync(string userId);
    }
}
