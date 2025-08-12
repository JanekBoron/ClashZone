using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Repository
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly ApplicationDbContext _db;
        public ProductsRepository(ApplicationDbContext db) => _db = db;

        public Task<Product?> GetAsync(int id) =>
            _db.Products.Include(p => p.Images).Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

        public Task<IReadOnlyList<Product>> GetAllAsync() =>
            _db.Products.Include(p => p.Category).Include(p => p.Images)
                .OrderByDescending(p => p.Id).ToListAsync()
                .ContinueWith(t => (IReadOnlyList<Product>)t.Result);

        public Task<IReadOnlyList<Product>> GetActiveAsync() =>
            _db.Products.Include(p => p.Images).Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.LimitedEdition).ThenBy(p => p.ClashCoins)
                .ToListAsync().ContinueWith(t => (IReadOnlyList<Product>)t.Result);

        public async Task AddAsync(Product product)
            => await _db.Products.AddAsync(product);

        public Task UpdateAsync(Product product)
        {
            _db.Products.Update(product);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Product product)
        {
            _db.Products.Remove(product);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
