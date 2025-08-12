using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(160)]
        public string Name { get; set; } = default!;

        [MaxLength(512)]
        public string? ShortDescription { get; set; }

        public string? LongDescription { get; set; }

        [Range(0, int.MaxValue)]
        public int ClashCoins { get; set; }

        public bool LimitedEdition { get; set; }

        public int? Stock { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(Category))]
        public int? CategoryId { get; set; }
        public ProductCategory? Category { get; set; }

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}
