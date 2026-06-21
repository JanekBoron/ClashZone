namespace ClashZone.App.Services.Interfaces
{
    public interface ICoinWalletService
    {
        Task<int> GetBalanceAsync(string userId);
        Task CreditAsync(string userId, int amount, string reference);
    }
}
