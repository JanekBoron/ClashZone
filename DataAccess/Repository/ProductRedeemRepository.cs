using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
using DataAccess;

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
    }
}

