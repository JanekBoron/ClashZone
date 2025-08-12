using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class ProductCategory
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(80)]
      //  [Index(IsUnique = true)] // wymaga pakietu EFCore.IndexAttribute
        public string Name { get; set; } = default!;

        [MaxLength(160)]
        public string? Slug { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
