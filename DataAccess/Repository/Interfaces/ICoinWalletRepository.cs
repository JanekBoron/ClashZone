using ClashZone.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Repository.Interfaces
{
    public interface ICoinWalletRepository
    {
        Task<CoinWallet> GetOrCreateAsync(string userId);
        Task AddTransactionAsync(CoinWalletTransaction tx);
        Task<int> GetBalanceAsync(string userId);
        Task SaveChangesAsync();
    }
}
