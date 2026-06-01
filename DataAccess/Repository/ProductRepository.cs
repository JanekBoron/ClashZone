using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.DataAccess.Repository
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        public ProductsRepository(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;  
        }

        public async Task<ProductDomain?> GetAsync(int id)
        {
            var product = await _db.Products.Include(p => p.Images).Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
            return _mapper.Map<ProductDomain?>(product);
        }

        public async Task<IReadOnlyList<ProductDomain>> GetAllAsync()
        {
            var products = await _db.Products.Include(p => p.Category).Include(p => p.Images)
                .OrderByDescending(p => p.Id).ToListAsync();
            return _mapper.Map<IReadOnlyList<ProductDomain>>(products);
        }

        public async Task<IReadOnlyList<ProductDomain>> GetActiveAsync()
        {
            var activeProducts = await _db.Products.Include(p => p.Images).Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.LimitedEdition).ThenBy(p => p.ClashCoins)
                .ToListAsync();
            return _mapper.Map<IReadOnlyList<ProductDomain>>(activeProducts);
        }

        public async Task AddAsync(ProductDomain product)
        {
            var productEntity = _mapper.Map<Product>(product);
            await _db.Products.AddAsync(productEntity);
        }

        public async Task UpdateAsync(ProductDomain product)
        {
            var existing = await _db.Products.FindAsync(product.Id);
            if (existing == null) return;
            _mapper.Map(product, existing);
        }

        public async Task DeleteAsync(ProductDomain product)
        {
            var existing = await _db.Products.FindAsync(product.Id);
            if (existing != null)
                _db.Products.Remove(existing);
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
