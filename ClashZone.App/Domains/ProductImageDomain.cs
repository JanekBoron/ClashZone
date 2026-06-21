using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class ProductImageDomain
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public ProductDomain Product { get; set; } = default!;
        public string Url { get; set; } = default!;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; } = 0;
    }
}
