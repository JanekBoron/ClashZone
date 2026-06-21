
using ClashZone.App.Domains;

namespace ClashZone.App.Services.Interfaces
{
    public interface ICoinShopService
    {
        Task<IReadOnlyList<ProductDomain>> GetCatalogAsync();
        Task<ProductDomain?> GetProductAsync(int id);
        Task<(bool ok, string? error)> PurchaseAsync(int productId, string userId, ShippingAddressDomain shipping);
    }
}
