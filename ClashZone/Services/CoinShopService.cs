using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;

namespace ClashZone.Services
{
    public class CoinShopService : ICoinShopService
    {
        private readonly IProductsRepository _products;
        private readonly ICoinWalletRepository _wallets;
        private readonly IProductRedeemRepository _redemptions;

        public CoinShopService(
            IProductsRepository products,
            ICoinWalletRepository wallets,
            IProductRedeemRepository redemptions)
        {
            _products = products;
            _wallets = wallets;
            _redemptions = redemptions;
        }

        public Task<IReadOnlyList<Product>> GetCatalogAsync() =>
            _products.GetActiveAsync();

        public Task<Product?> GetProductAsync(int id) =>
            _products.GetAsync(id);

        public async Task<(bool ok, string? error)> PurchaseAsync(int productId, string userId)
        {
            var product = await _products.GetAsync(productId);
            if (product == null || !product.IsActive) return (false, "Produkt niedostępny.");
            if (product.Stock.HasValue && product.Stock.Value <= 0) return (false, "Brak w magazynie.");

            var balance = await _wallets.GetBalanceAsync(userId);
            if (balance < product.ClashCoins) return (false, "Za mało Clash Coins.");

            await _wallets.AddTransactionAsync(new CoinWalletTransaction
            {
                UserId = userId,
                ProductId = product.Id,
                Type = CoinWalletTransactionType.Spend,
                Amount = product.ClashCoins,
                Reference = $"Product:{product.Id}"
            });

            await _redemptions.AddAsync(new ProductRedeem
            {
                UserId = userId,
                ProductId = product.Id,
                Status = "Pending"
            });

            if (product.Stock.HasValue) product.Stock--;

            await _wallets.SaveChangesAsync();
            await _products.UpdateAsync(product);
            await _products.SaveChangesAsync();
            await _redemptions.SaveChangesAsync();

            return (true, null);
        }

    }
}
