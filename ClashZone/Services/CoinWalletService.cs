using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;

namespace ClashZone.Services
{
    public class CoinWalletService : ICoinWalletService
    {
        private readonly ICoinWalletRepository _wallets;

        public CoinWalletService(ICoinWalletRepository wallets)
        {
            _wallets = wallets;
        }

        public Task<int> GetBalanceAsync(string userId)
        {
            return _wallets.GetBalanceAsync(userId);
        }
            
        public async Task CreditAsync(string userId, int amount, string reference)
        {
            await _wallets.AddTransactionAsync(new CoinWalletTransaction
            {
                UserId = userId,
                Type = CoinWalletTransactionType.Earn,
                Amount = amount,
                Reference = reference
            });

            await _wallets.SaveChangesAsync();
        }
    }
}

