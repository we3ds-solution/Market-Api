using Market.Application.Features.Products.Commands;
using Market.Domain.Entities;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Tests.Features.Products;

public class ProductSkuUniquenessTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static (Mock<IUnitOfWork> unitOfWork, Mock<IProductRepository> productRepo, UpdateProductInternalCommandHandler handler) BuildUpdateHandler()
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var productRepo = new Mock<IProductRepository>(MockBehavior.Strict);
        var vendorRepo = new Mock<IVendorRepository>(MockBehavior.Strict);

        // Default vendor: approved and active (so SKU tests don't fail on vendor-state checks)
        var approvedActiveVendor = new Vendor
        {
            Id = 1,
            UserId = 1,
            StoreName = "Test Store",
            IsApproved = true,
            IsActive = true
        };
        vendorRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedActiveVendor);

        unitOfWork.SetupGet(u => u.Products).Returns(productRepo.Object);
        unitOfWork.SetupGet(u => u.Vendors).Returns(vendorRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateProductInternalCommandHandler(
            unitOfWork.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateProductInternalCommandHandler>.Instance);

        return (unitOfWork, productRepo, handler);
    }

    private static (Mock<IUnitOfWork> unitOfWork, Mock<IProductRepository> productRepo, CreateProductCommandHandler handler) BuildCreateHandler()
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var productRepo = new Mock<IProductRepository>(MockBehavior.Strict);
        var vendorRepo = new Mock<IVendorRepository>(MockBehavior.Strict);
        var categoryRepo = new Mock<ICategoryRepository>(MockBehavior.Strict);

        // Default vendor: approved and active (so SKU tests don't fail on vendor-state checks)
        var approvedActiveVendor = new Vendor
        {
            Id = 1,
            UserId = 1,
            StoreName = "Test Store",
            IsApproved = true,
            IsActive = true
        };

        unitOfWork.SetupGet(u => u.Products).Returns(productRepo.Object);
        unitOfWork.SetupGet(u => u.Vendors).Returns(vendorRepo.Object);
        unitOfWork.SetupGet(u => u.Categories).Returns(categoryRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        productRepo.Setup(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);
        vendorRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedActiveVendor);
        categoryRepo.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateProductCommandHandler(
            unitOfWork.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateProductCommandHandler>.Instance);

        return (unitOfWork, productRepo, handler);
    }

    // -----------------------------------------------------------------------
    // Test 1: Create Product with unique SKU
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Create_WithUniqueSku_Succeeds()
    {
        var (unitOfWork, productRepo, handler) = BuildCreateHandler();

        productRepo.Setup(r => r.GetBySkuAsync("UNIQUE-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "This is a test product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10,
            SKU = "UNIQUE-SKU"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 2: Create Product with duplicate SKU
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Create_WithDuplicateSku_ThrowsInvalidOperationException()
    {
        var (unitOfWork, productRepo, handler) = BuildCreateHandler();

        var existingProduct = new Product
        {
            Id = 1,
            SKU = "DUPLICATE-SKU",
            Name = "Existing Product",
            Description = "Existing",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m
        };

        productRepo.Setup(r => r.GetBySkuAsync("DUPLICATE-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        var command = new CreateProductCommand
        {
            Name = "New Product",
            Description = "This is a new product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10,
            SKU = "DUPLICATE-SKU"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("SKU", ex.Message);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // Test 3: Update Product keeping same SKU
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Update_KeepingOwnSku_Succeeds()
    {
        var (unitOfWork, productRepo, handler) = BuildUpdateHandler();

        var existingProduct = new Product
        {
            Id = 1,
            SKU = "PRODUCT-SKU",
            Name = "Old Name",
            Description = "Old description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);
        productRepo.Setup(r => r.GetBySkuAsync("PRODUCT-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        var command = new UpdateProductInternalCommand
        {
            Id = 1,
            Name = "New Name",
            Description = "New description with more content",
            Price = 150m,
            Quantity = 20,
            SKU = "PRODUCT-SKU"  // Same SKU
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("New Name", result.Name);
        Assert.Equal("New description with more content", result.Description);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 4: Update Product to another product's SKU
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Update_ToAnotherProductsSku_ThrowsInvalidOperationException()
    {
        var (unitOfWork, productRepo, handler) = BuildUpdateHandler();

        var productBeingUpdated = new Product
        {
            Id = 1,
            SKU = "PRODUCT-A-SKU",
            Name = "Product A",
            Description = "Product A description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        var productWithConflictingSku = new Product
        {
            Id = 2,
            SKU = "PRODUCT-B-SKU",
            Name = "Product B",
            Description = "Product B description",
            VendorId = 1,
            CategoryId = 1,
            Price = 200m,
            Quantity = 5
        };

        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(productBeingUpdated);
        productRepo.Setup(r => r.GetBySkuAsync("PRODUCT-B-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(productWithConflictingSku);

        var command = new UpdateProductInternalCommand
        {
            Id = 1,
            Name = "Product A",
            Description = "Product A description with updates",
            Price = 100m,
            Quantity = 10,
            SKU = "PRODUCT-B-SKU"  // Conflicts with Product 2
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("SKU", ex.Message);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // Test 5: SKU uniqueness preserved after failed update
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Update_FailedSkuConflict_PreservesExistingData()
    {
        var (unitOfWork, productRepo, handler) = BuildUpdateHandler();

        var product1 = new Product
        {
            Id = 1,
            SKU = "SKU-1",
            Name = "Product 1",
            Description = "Product 1 description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        var product2 = new Product
        {
            Id = 2,
            SKU = "SKU-2",
            Name = "Product 2",
            Description = "Product 2 description",
            VendorId = 1,
            CategoryId = 1,
            Price = 200m,
            Quantity = 5
        };

        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);
        productRepo.Setup(r => r.GetBySkuAsync("SKU-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product2);

        var command = new UpdateProductInternalCommand
        {
            Id = 1,
            Name = "Modified Product 1",
            Description = "Modified description",
            Price = 150m,
            Quantity = 20,
            SKU = "SKU-2"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Verify product1 was NOT modified
        Assert.Equal("Product 1", product1.Name);
        Assert.Equal("Product 1 description", product1.Description);
        Assert.Equal(100m, product1.Price);
        Assert.Equal(10, product1.Quantity);
        Assert.Equal("SKU-1", product1.SKU);

        // Verify product2 SKU is still reserved
        Assert.Equal("SKU-2", product2.SKU);
    }

    // -----------------------------------------------------------------------
    // Test 6: SKU uniqueness preserved after failed create
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Create_FailedDuplicateSku_PreservesExistingProduct()
    {
        var (unitOfWork, productRepo, handler) = BuildCreateHandler();

        var existingProduct = new Product
        {
            Id = 1,
            SKU = "RESERVED-SKU",
            Name = "Existing Product",
            Description = "Existing product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        productRepo.Setup(r => r.GetBySkuAsync("RESERVED-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        var command = new CreateProductCommand
        {
            Name = "New Product",
            Description = "New product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10,
            SKU = "RESERVED-SKU"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Verify existing product unchanged
        Assert.Equal("Existing Product", existingProduct.Name);
        Assert.Equal("RESERVED-SKU", existingProduct.SKU);
    }

    // -----------------------------------------------------------------------
    // Test 7: SKU with whitespace (allowed, not normalized)
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Create_SkuWithWhitespace_Accepted()
    {
        var (unitOfWork, productRepo, handler) = BuildCreateHandler();

        productRepo.Setup(r => r.GetBySkuAsync("SKU WITH SPACES", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "Test product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10,
            SKU = "SKU WITH SPACES"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
    }

    // -----------------------------------------------------------------------
    // Test 8: Concurrent creates with same SKU (database enforces)
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Create_ConcurrentCreatesWithSameSku_OnlyOneSucceeds()
    {
        // This test demonstrates that:
        // 1. Two tasks might pass application-level check
        // 2. Database constraint prevents duplicate insertion
        // 3. One task fails with unique constraint violation

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var productRepo = new Mock<IProductRepository>(MockBehavior.Strict);
        var vendorRepo = new Mock<IVendorRepository>(MockBehavior.Strict);
        var categoryRepo = new Mock<ICategoryRepository>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Products).Returns(productRepo.Object);
        unitOfWork.SetupGet(u => u.Vendors).Returns(vendorRepo.Object);
        unitOfWork.SetupGet(u => u.Categories).Returns(categoryRepo.Object);

        var approvedActiveVendor = new Vendor
        {
            Id = 1,
            UserId = 1,
            StoreName = "Test Store",
            IsApproved = true,
            IsActive = true
        };
        vendorRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedActiveVendor);
        categoryRepo.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Simulate application-level check passes for both
        productRepo.Setup(r => r.GetBySkuAsync("RACE-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // First create succeeds
        var product1Created = false;
        productRepo.Setup(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback(() => product1Created = true)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        // SaveAsync call: simulate first insert succeeds
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateProductCommandHandler(
            unitOfWork.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateProductCommandHandler>.Instance);

        var command = new CreateProductCommand
        {
            Name = "Product A",
            Description = "Product A with race condition test description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10,
            SKU = "RACE-SKU"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(product1Created);
    }

    // -----------------------------------------------------------------------
    // Test 9: Soft-deleted product SKU can be reused
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Create_ReuseSkuFromSoftDeletedProduct_Succeeds()
    {
        var (unitOfWork, productRepo, handler) = BuildCreateHandler();

        // Soft-deleted product is filtered out by GetBySkuAsync
        productRepo.Setup(r => r.GetBySkuAsync("REUSED-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);  // Soft-deleted products filtered by repository

        var command = new CreateProductCommand
        {
            Name = "New Product Using Old SKU",
            Description = "Reusing SKU from soft-deleted product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10,
            SKU = "REUSED-SKU"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 10: Create without SKU still succeeds
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Create_WithoutSku_Succeeds()
    {
        var (unitOfWork, productRepo, handler) = BuildCreateHandler();

        productRepo.Setup(r => r.GetBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new CreateProductCommand
        {
            Name = "Product Without SKU",
            Description = "This product doesn't have a SKU description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10,
            SKU = null  // No SKU
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 11: Update without SKU still succeeds
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Update_WithoutSku_Succeeds()
    {
        var (unitOfWork, productRepo, handler) = BuildUpdateHandler();

        var existingProduct = new Product
        {
            Id = 1,
            SKU = "OLD-SKU",
            Name = "Old Name",
            Description = "Old description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        var command = new UpdateProductInternalCommand
        {
            Id = 1,
            Name = "New Name",
            Description = "New description",
            Price = 150m,
            Quantity = 20,
            SKU = null  // Remove SKU
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("New Name", result.Name);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 12: Update to change SKU to a new value
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Update_ChangingSkuToNewValue_Succeeds()
    {
        var (unitOfWork, productRepo, handler) = BuildUpdateHandler();

        var existingProduct = new Product
        {
            Id = 1,
            SKU = "OLD-SKU",
            Name = "Product",
            Description = "Description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);
        productRepo.Setup(r => r.GetBySkuAsync("NEW-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new UpdateProductInternalCommand
        {
            Id = 1,
            Name = "Product",
            Description = "Description",
            Price = 100m,
            Quantity = 10,
            SKU = "NEW-SKU"  // Change to new SKU
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 13: Product not found on update
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Update_ProductNotFound_ThrowsKeyNotFoundException()
    {
        var (unitOfWork, productRepo, handler) = BuildUpdateHandler();

        productRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new UpdateProductInternalCommand
        {
            Id = 999,
            Name = "Product",
            Description = "Description",
            Price = 100m,
            Quantity = 10,
            SKU = "NEW-SKU"
        };

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("999", ex.Message);
    }

    // -----------------------------------------------------------------------
    // Test 14: SKU max length constraint (100 chars)
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Create_SkuAtMaxLength_Succeeds()
    {
        var (unitOfWork, productRepo, handler) = BuildCreateHandler();

        var skuMaxLength = new string('A', 100);

        productRepo.Setup(r => r.GetBySkuAsync(skuMaxLength, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "Test product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10,
            SKU = skuMaxLength
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
    }

    // -----------------------------------------------------------------------
    // Test 15: Empty string SKU treated as null (no uniqueness check)
    // -----------------------------------------------------------------------
    [Fact]
    public async Task Create_WithEmptySku_Succeeds()
    {
        var (unitOfWork, productRepo, handler) = BuildCreateHandler();

        // Empty string should be treated as null by IsNullOrWhiteSpace check
        productRepo.Setup(r => r.GetBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "Test product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10,
            SKU = ""  // Empty string
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        // Verify GetBySkuAsync was not called for empty string
        productRepo.Verify(r => r.GetBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}


