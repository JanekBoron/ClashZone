

namespace ClashZone.App.Domains
{
    public class ProductDomain
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public string? ShortDescription { get; set; }

        public string? LongDescription { get; set; }

        public int ClashCoins { get; set; }

        public bool LimitedEdition { get; set; }

        public int? Stock { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CategoryId { get; set; }
        public ProductCategoryDomain? Category { get; set; }

        public ICollection<ProductImageDomain> Images { get; set; } = new List<ProductImageDomain>();
    }
}
