using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.DataAccess.Repository
{
    public class ProductRedeemRepository : IProductRedeemRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        public ProductRedeemRepository(ApplicationDbContext db, IMapper mapper)
        {
            _db= db;
            _mapper= mapper;
        }

        public async Task AddAsync(ProductRedeemDomain redeem)
        {
            var addition = _mapper.Map<ProductRedeem>(redeem);
            await _db.ProductRedeems.AddAsync(addition);
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();

        public async Task<List<ProductRedeemDomain>> GetByUserAsync(string userId)
        {
            var orders = await _db.ProductRedeems
                .Include(r => r.Product)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RedeemedAt)
                .ToListAsync();
            return _mapper.Map<List<ProductRedeemDomain>>(orders);
        }

        public async Task<List<ProductRedeemDomain>> GetAllAsync()
        {
            var orders = await _db.ProductRedeems
                .Include(r => r.Product)
                .OrderByDescending(r => r.RedeemedAt)
                .ToListAsync();
            return _mapper.Map<List<ProductRedeemDomain>>(orders);
        }

        public async Task<ProductRedeemDomain?> GetByIdAsync(int id)
        {
            var order = await _db.ProductRedeems
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == id);
            return _mapper.Map<ProductRedeemDomain?>(order);
        }

        public async Task UpdateStatusAsync(int id, string status, string? notes)
        {
            var order = await _db.ProductRedeems.FindAsync(id);
            if (order == null) return;
            order.Status = status;
            if (notes != null)
            {
                order.Notes = notes;
            }
            await _db.SaveChangesAsync();
        }
    }
}

