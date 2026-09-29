using Market.Domain.Common;
using Market.Domain.Enums;
using Market.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Market.Domain.Entities
{
    /// <summary>
    /// Product entity for e-commerce platform
    /// </summary>
    public class Product : BaseEntity
    {
        public int VendorId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Quantity { get; set; }
        public int Sold { get; set; } = 0;

        public string? SubCategory { get; set; }
        public string? SKU { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public decimal AverageRating { get; set; } = 0.00m;
        public int ReviewCount { get; set; } = 0;

        /// <summary>
        /// Row version for optimistic concurrency control on inventory updates.
        /// EF Core uses this to detect concurrent modifications to Product.Quantity/Sold.
        /// Automatically managed by SQL Server (rowversion type).
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation properties
        public Vendor Vendor { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public ICollection<ProductTag> Tags { get; set; } = new List<ProductTag>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        /// <summary>
        /// Add tags to the product
        /// </summary>
        public void AddTags(params string[] tagNames)
        {
            foreach (var name in tagNames)
            {
                var tag = Tag.Create(name);
                if (!Tags.Any(t => t.TagName.Equals(tag.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    Tags.Add(new ProductTag { TagName = tag.Name });
                }
            }
        }

        /// <summary>
        /// Remove a tag from the product
        /// </summary>
        public void RemoveTag(string tagName)
        {
            var tag = Tags.FirstOrDefault(t => t.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            if (tag != null)
            {
                Tags.Remove(tag);
            }
        }

        /// <summary>
        /// Get calculated profit after commission
        /// </summary>
        public decimal CalculateProfit(decimal commissionRate)
        {
            return Price * (1 - commissionRate);
        }

        /// <summary>
        /// Check if product is in stock
        /// </summary>
        public bool IsInStock => Quantity > 0 && Status == ProductStatus.Active;
    }
}

