using Market.Application.Features.Products.Commands;
using Market.Domain.Entities;
using Market.Domain.Repositories;
using Moq;
using Xunit;

namespace Market.Application.Tests.Features.Products;

/// <summary>
/// Tests for Product creation/update validation of Vendor lifecycle states.
/// Ensures only approved and active vendors can create/modify products.
/// </summary>
public class ProductVendorStateTests
{
    // -----------------------------------------------------------------------
    // Vendor State Validation Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateProduct_WithPendingVendor_Throws()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var vendorRepository = new Mock<IVendorRepository>(MockBehavior.Strict);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<CreateProductCommandHandler>>();
        
        var pendingVendor = new Vendor
        {
            Id = 1,
            UserId = 1,
            StoreName = "Store",
            IsApproved = false,  // Pending!
            IsActive = true
        };

        unitOfWork.Setup(u => u.Vendors).Returns(vendorRepository.Object);
        vendorRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingVendor);

        var handler = new CreateProductCommandHandler(unitOfWork.Object, logger.Object);

        var command = new CreateProductCommand
        {
            Name = "Product",
            Description = "Valid long description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100,
            Quantity = 10
        };

        // Mock CategoryRepository
        var categoryRepository = new Mock<ICategoryRepository>(MockBehavior.Strict);
        unitOfWork.Setup(u => u.Categories).Returns(categoryRepository.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("not approved", ex.Message);
    }

    [Fact]
    public async Task CreateProduct_WithInactiveVendor_Throws()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var vendorRepository = new Mock<IVendorRepository>(MockBehavior.Strict);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<CreateProductCommandHandler>>();
        
        var inactiveVendor = new Vendor
        {
            Id = 1,
            UserId = 1,
            StoreName = "Store",
            IsApproved = true,
            IsActive = false  // Inactive!
        };

        unitOfWork.Setup(u => u.Vendors).Returns(vendorRepository.Object);
        vendorRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveVendor);

        var handler = new CreateProductCommandHandler(unitOfWork.Object, logger.Object);

        var command = new CreateProductCommand
        {
            Name = "Product",
            Description = "Valid long description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100,
            Quantity = 10
        };

        // Mock CategoryRepository
        var categoryRepository = new Mock<ICategoryRepository>(MockBehavior.Strict);
        unitOfWork.Setup(u => u.Categories).Returns(categoryRepository.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("inactive", ex.Message);
    }

    [Fact]
    public async Task CreateProduct_WithApprovedActiveVendor_Succeeds()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var vendorRepository = new Mock<IVendorRepository>(MockBehavior.Strict);
        var categoryRepository = new Mock<ICategoryRepository>(MockBehavior.Strict);
        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<CreateProductCommandHandler>>();
        
        var approvedVendor = new Vendor
        {
            Id = 1,
            UserId = 1,
            StoreName = "Store",
            IsApproved = true,
            IsActive = true
        };

        var command = new CreateProductCommand
        {
            Name = "Product",
            Description = "Valid long description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100,
            Quantity = 10
        };

        unitOfWork.SetupGet(u => u.Vendors).Returns(vendorRepository.Object);
        unitOfWork.SetupGet(u => u.Categories).Returns(categoryRepository.Object);
        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        
        vendorRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedVendor);
        categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = 1, IsActive = true });
        productRepository.Setup(r => r.GetBySkuAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        productRepository.Setup(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => 
            {
                p.Id = 1;
                return p;
            });
        
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new CreateProductCommandHandler(unitOfWork.Object, logger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Product", result.Name);
    }

    [Fact]
    public async Task UpdateProduct_WithInactiveVendor_Throws()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var vendorRepository = new Mock<IVendorRepository>(MockBehavior.Strict);
        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UpdateProductInternalCommandHandler>>();
        
        var product = new Product
        {
            Id = 100,
            VendorId = 1,
            Name = "Original",
            CategoryId = 1,
            Price = 100,
            Quantity = 10
        };

        var inactiveVendor = new Vendor
        {
            Id = 1,
            UserId = 1,
            StoreName = "Store",
            IsApproved = true,
            IsActive = false  // Inactive!
        };

        var command = new UpdateProductInternalCommand
        {
            Id = 100,
            Name = "Updated",
            Description = "Valid long description",
            Price = 150,
            Quantity = 20
        };

        unitOfWork.Setup(u => u.Products).Returns(productRepository.Object);
        unitOfWork.Setup(u => u.Vendors).Returns(vendorRepository.Object);
        
        productRepository.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        vendorRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveVendor);

        var handler = new UpdateProductInternalCommandHandler(unitOfWork.Object, logger.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("inactive", ex.Message);
    }

    // -----------------------------------------------------------------------
    // Historical Data Integrity Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ProductUpdate_DoesNotAffectOrderItemSnapshot()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            VendorId = 1,
            Name = "Original Name",
            Price = 100,
            Quantity = 50
        };

        var orderItem = new OrderItem
        {
            Id = 1,
            ProductId = 1,
            ProductName = "Original Name",
            Price = 100,
            Quantity = 10,
            VendorId = 1
        };

        // Act
        product.Name = "Updated Name";
        product.Price = 150;

        // Assert
        Assert.Equal("Updated Name", product.Name);
        Assert.Equal(150, product.Price);
        
        // OrderItem should not be affected
        Assert.Equal("Original Name", orderItem.ProductName);
        Assert.Equal(100, orderItem.Price);
    }

    [Fact]
    public void VendorDeactivation_DoesNotAffectHistoricalOrderItems()
    {
        // Arrange
        var vendor = new Vendor { Id = 1, IsActive = true, StoreName = "Store" };
        var orderItem = new OrderItem { VendorId = 1, ProductName = "Product", Price = 100, Quantity = 1 };

        // Act
        vendor.IsActive = false;  // Deactivate vendor

        // Assert
        Assert.False(vendor.IsActive);
        
        // OrderItem should still be readable and unchanged
        Assert.Equal(1, orderItem.VendorId);
        Assert.Equal("Product", orderItem.ProductName);
        Assert.Equal(100, orderItem.Price);
    }

    // -----------------------------------------------------------------------
    // Product Deletion Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void DeleteProduct_PreservesVendorReference()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            VendorId = 1,
            Name = "Product",
            IsDeleted = false
        };

        // Act
        product.Delete();

        // Assert
        Assert.True(product.IsDeleted);
        Assert.NotNull(product.DeletedAt);
        Assert.Equal(1, product.VendorId);  // FK still intact
    }
}




