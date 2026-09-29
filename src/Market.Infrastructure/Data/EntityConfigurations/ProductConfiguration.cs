using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Data.EntityConfigurations
{
    /// <summary>
    /// EF Core configuration for Product entity
    /// </summary>
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Primary key
            builder.HasKey(p => p.Id);

            // Properties
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(p => p.Description)
                .HasMaxLength(2000);

            builder.Property(p => p.ImageUrl)
                .HasMaxLength(500);

            builder.Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Property(p => p.DiscountPrice)
                .HasPrecision(18, 2);

            builder.Property(p => p.SKU)
                .HasMaxLength(100);

            builder.Property(p => p.Status)
                .HasConversion<int>();

            builder.Property(p => p.AverageRating)
                .HasPrecision(3, 2);  // Ratings are stored exactly to two decimal places.

            /// <summary>
            /// Configure row version for optimistic concurrency control on inventory updates.
            /// SQL Server automatically manages the rowversion type.
            /// EF Core uses this in WHERE clause during UPDATE to detect concurrent modifications.
            /// </summary>
            builder.Property(p => p.RowVersion)
                .IsRowVersion();

            // Indexes
            builder.HasIndex(p => p.VendorId);
            builder.HasIndex(p => p.CategoryId);

            builder.HasIndex(p => p.SKU)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [SKU] IS NOT NULL");

            builder.HasIndex(p => p.Status);

            // Relationships — all configured here on the dependent (Product) side
            // Product → Category (Product is dependent, owns CategoryId FK)
            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → Vendor (Product is dependent, owns VendorId FK)
            builder.HasOne(p => p.Vendor)
                .WithMany(v => v.Products)
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductTag → Product (ProductTag is dependent, owns ProductId FK)
            // Not configured in ProductTagConfiguration — single source of truth here.
            builder.HasMany(p => p.Tags)
                .WithOne(pt => pt.Product)
                .HasForeignKey(pt => pt.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // NOTE: CartItem→Product, OrderItem→Product, and Review→Product relationships
            // are configured in CartItemConfiguration, OrderItemConfiguration, and
            // ReviewConfiguration respectively (dependent-side only).

            // Table
            builder.ToTable("Products");
        }
    }
}

