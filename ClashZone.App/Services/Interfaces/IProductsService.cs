using ClashZone.App.Domains;

namespace ClashZone.App.Services.Interfaces
{
    public interface IProductsService
    {
        Task<IReadOnlyList<ProductDomain>> GetAllAsync();
        Task<ProductDomain?> GetAsync(int id);
        Task CreateAsync(ProductDomain product, string? primaryImageUrl);
        Task UpdateAsync(ProductDomain product, string? primaryImageUrl);
        Task DeleteAsync(int id);
    }
}
