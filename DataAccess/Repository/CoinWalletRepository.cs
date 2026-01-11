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
    public class CoinWalletRepository : ICoinWalletRepository
    {
        private readonly ApplicationDbContext _db;
        public CoinWalletRepository(ApplicationDbContext db) => _db = db;

        public async Task<CoinWallet> GetOrCreateAsync(string userId)
        {
            var w = await _db.CoinWallets.Include(x => x.Transactions)
                                     .FirstOrDefaultAsync(x => x.UserId == userId);
            if (w != null) return w;
            w = new CoinWallet { UserId = userId, Balance = 0 };
            _db.CoinWallets.Add(w);
            await _db.SaveChangesAsync();
            return w;
        }

        public Task<int> GetBalanceAsync(string userId) =>
            _db.CoinWallets.Where(w => w.UserId == userId).Select(w => w.Balance).FirstOrDefaultAsync();

        public async Task AddTransactionAsync(CoinWalletTransaction tx)
        {
            var wallet = await GetOrCreateAsync(tx.UserId);
            wallet.Balance += tx.Type == CoinWalletTransactionType.Earn ? tx.Amount : -tx.Amount;
            await _db.CoinWalletTransactions.AddAsync(tx);
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();

        public async Task<List<Product>> GetPurchasedProductsAsync(string userId)
        {
            var productIds = await _db.CoinWalletTransactions
                .Where(tx => tx.UserId == userId && tx.ProductId > 0)
                .Select(tx => tx.ProductId)
                .ToListAsync();

            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            return products;
        }
    }
}

