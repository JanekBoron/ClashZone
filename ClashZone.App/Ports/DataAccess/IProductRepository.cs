using ClashZone.App.Domains;


namespace ClashZone.App.Ports.DataAccess
{
    public interface IProductsRepository
    {
        Task<ProductDomain?> GetAsync(int id);
        Task<IReadOnlyList<ProductDomain>> GetAllAsync();
        Task<IReadOnlyList<ProductDomain>> GetActiveAsync();

        Task AddAsync(ProductDomain product);
        Task UpdateAsync(ProductDomain product);
        Task DeleteAsync(ProductDomain product);

        Task SaveChangesAsync();
    }
}
