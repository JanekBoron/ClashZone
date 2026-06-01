using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface IProductRedeemRepository
    {
        Task AddAsync(ProductRedeemDomain redemption);
        Task SaveChangesAsync();
    }
}
