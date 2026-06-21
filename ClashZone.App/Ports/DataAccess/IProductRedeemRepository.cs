using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface IProductRedeemRepository
    {
        Task AddAsync(ProductRedeemDomain redemption);
        Task SaveChangesAsync();
        Task<List<ProductRedeemDomain>> GetByUserAsync(string userId);
        Task<List<ProductRedeemDomain>> GetAllAsync();
        Task<ProductRedeemDomain?> GetByIdAsync(int id);
        Task UpdateStatusAsync(int id, string status, string? notes);
    }
}
