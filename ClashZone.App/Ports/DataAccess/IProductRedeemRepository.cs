using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface IProductRedeemRepository
    {
        Task AddAsync(ProductRedeemDomain redemption);
        Task SaveChangesAsync();

        /// <summary>Zamówienia danego użytkownika (z produktem), od najnowszych.</summary>
        Task<List<ProductRedeemDomain>> GetByUserAsync(string userId);

        /// <summary>Wszystkie zamówienia (z produktem) — dla panelu administratora.</summary>
        Task<List<ProductRedeemDomain>> GetAllAsync();

        /// <summary>Pojedyncze zamówienie wraz z produktem lub null.</summary>
        Task<ProductRedeemDomain?> GetByIdAsync(int id);

        /// <summary>
        /// Aktualizuje status (i opcjonalnie notatkę) zamówienia. Zapisuje zmiany.
        /// </summary>
        Task UpdateStatusAsync(int id, string status, string? notes);
    }
}
