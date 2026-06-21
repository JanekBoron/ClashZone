using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class ProductRedeemDomain
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public int ProductId { get; set; }
        public ProductDomain Product { get; set; } = default!;
        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
        public string? Notes { get; set; }

        public string? ShippingFullName { get; set; }
        public string? ShippingPhone { get; set; }
        public string? ShippingStreet { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingCountry { get; set; }
    }
}
