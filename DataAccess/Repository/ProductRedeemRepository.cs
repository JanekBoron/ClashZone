using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Repository
{
    public class ProductRedeemRepository : IProductRedeemRepository
    {
        private readonly ApplicationDbContext _db;
        public ProductRedeemRepository(ApplicationDbContext db) => _db = db;

        public async Task AddAsync(ProductRedeem redemption)
            => await _db.ProductRedeems.AddAsync(redemption);

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}

