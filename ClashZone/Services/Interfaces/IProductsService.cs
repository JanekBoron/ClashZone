using ClashZone.DataAccess.Models;

namespace ClashZone.Services.Interfaces
{
    public interface IProductsService
    {
        Task<IReadOnlyList<Product>> GetAllAsync();
        Task<Product?> GetAsync(int id);
        Task CreateAsync(Product product, string? primaryImageUrl);
        Task UpdateAsync(Product product, string? primaryImageUrl);
        Task DeleteAsync(int id);
    }
}
