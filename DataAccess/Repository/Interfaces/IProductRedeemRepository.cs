using ClashZone.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Repository.Interfaces
{
    public interface IProductRedeemRepository
    {
        Task AddAsync(ProductRedeem redemption);
        Task SaveChangesAsync();
    }
}
