using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class ProductRedeem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(80)]
        public string Status { get; set; } = "Pending";

        [MaxLength(256)]
        public string? Notes { get; set; }

        [MaxLength(120)]
        public string? ShippingFullName { get; set; }

        [MaxLength(30)]
        public string? ShippingPhone { get; set; }

        [MaxLength(160)]
        public string? ShippingStreet { get; set; }

        [MaxLength(12)]
        public string? ShippingPostalCode { get; set; }

        [MaxLength(80)]
        public string? ShippingCity { get; set; }

        [MaxLength(80)]
        public string? ShippingCountry { get; set; }
    }
}
