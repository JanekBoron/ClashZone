using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;

namespace ClashZone.App.Services
{
    public class ProductsService : IProductsService
    {
        private readonly IProductsRepository _products;

        public ProductsService(IProductsRepository products) => _products = products;

        public Task<IReadOnlyList<ProductDomain>> GetAllAsync()
            => _products.GetAllAsync();

        public Task<ProductDomain?> GetAsync(int id)
            => _products.GetAsync(id);

        public async Task CreateAsync(ProductDomain product, string? primaryImageUrl)
        {
            if (!string.IsNullOrWhiteSpace(primaryImageUrl))
                product.Images.Add(new ProductImageDomain { Url = primaryImageUrl, IsPrimary = true, SortOrder = 0 });

            await _products.AddAsync(product);
            await _products.SaveChangesAsync();
        }

        public async Task UpdateAsync(ProductDomain model, string? primaryImageUrl)
        {
            var p = await _products.GetAsync(model.Id);
            if (p == null) throw new KeyNotFoundException("Product not found");

            p.Name = model.Name;
            p.ShortDescription = model.ShortDescription;
            p.LongDescription = model.LongDescription;
            p.ClashCoins = model.ClashCoins;
            p.LimitedEdition = model.LimitedEdition;
            p.Stock = model.Stock;
            p.IsActive = model.IsActive;
            p.CategoryId = model.CategoryId;

            if (!string.IsNullOrWhiteSpace(primaryImageUrl))
            {
                var existingPrimary = p.Images.FirstOrDefault(i => i.IsPrimary);
                if (existingPrimary != null) existingPrimary.Url = primaryImageUrl;
                else p.Images.Add(new ProductImageDomain { Url = primaryImageUrl, IsPrimary = true, SortOrder = 0 });
            }

            await _products.UpdateAsync(p);
            await _products.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var p = await _products.GetAsync(id);
            if (p == null) return;
            await _products.DeleteAsync(p);
            await _products.SaveChangesAsync();
        }
    }
}
