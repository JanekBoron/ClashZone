using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.DataAccess.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;


namespace ClashZone.DataAccess.Repository
{
    public class CoinWalletRepository : ICoinWalletRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        public CoinWalletRepository(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<CoinWalletDomain> GetOrCreateAsync(string userId)
        {
            var wallet = await GetOrCreateEntityAsync(userId);
            return _mapper.Map<CoinWalletDomain>(wallet);
        }

        private async Task<CoinWallet> GetOrCreateEntityAsync(string userId)
        {
            var wallet = await _db.CoinWallets.Include(x => x.Transactions)
                                     .FirstOrDefaultAsync(x => x.UserId == userId);
            if (wallet == null)
            {
                wallet = new CoinWallet { UserId = userId, Balance = 0 };
                _db.CoinWallets.Add(wallet);
                await _db.SaveChangesAsync();
            }
            return wallet;
        }

        public Task<int> GetBalanceAsync(string userId) =>
            _db.CoinWallets.Where(w => w.UserId == userId).Select(w => w.Balance).FirstOrDefaultAsync();

        public async Task AddTransactionAsync(CoinWalletTransactionDomain tx)
        {
            var transaction = _mapper.Map<CoinWalletTransaction>(tx);
            var wallet = await GetOrCreateEntityAsync(tx.UserId);
            wallet.Balance += tx.Type == App.Domains.CoinWalletTransactionType.Earn ? tx.Amount : -tx.Amount;
            await _db.CoinWalletTransactions.AddAsync(transaction);
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();

        public async Task<List<ProductDomain>> GetPurchasedProductsAsync(string userId)
        {
            var productIds = await _db.CoinWalletTransactions
                .Where(tx => tx.UserId == userId && tx.ProductId > 0)
                .Select(tx => tx.ProductId)
                .ToListAsync();

            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            return _mapper.Map<List<ProductDomain>> (products);
        }
    }
}

