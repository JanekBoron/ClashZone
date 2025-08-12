using ClashZone.DataAccess.Models;

namespace ClashZone.Services.Interfaces
{
    public interface ICoinShopService
    {
        Task<IReadOnlyList<Product>> GetCatalogAsync();
        Task<Product?> GetProductAsync(int id);
        Task<(bool ok, string? error)> PurchaseAsync(int productId, string userId);
    }
}
