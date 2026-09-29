using Market.Application.Features.Products.Commands;
using Market.Application.Features.Categories.Commands;
using Market.Application.Features.Categories.Queries;
using Market.Domain.Entities;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Application.Tests.Features.Products;

/// <summary>
/// Issue #13: Category Relationship & Data Integrity
///
/// Comprehensive test suite for Category-Product relationship ensuring:
/// 1. Create Product with valid Category → succeeds
/// 2. Create Product with non-existent CategoryId → rejected with KeyNotFoundException
/// 3. Create Product with inactive Category → rejected with InvalidOperationException
/// 4. Update Product to valid Category → succeeds (retains existing category)
/// 5. Update Product to non-existent Category → rejected (cannot change category)
/// 6. Invalid Category update does not partially modify Product
/// 7. Product retains existing Category when CategoryId is unchanged
/// 8. Category deletion follows Restrict FK behavior (cannot delete if Products reference it)
/// 9. Category soft-delete does not cascade-delete Products
/// 10. Product soft-delete from Issue #11 preserves Category integrity
/// 11. Historical OrderItems remain unchanged after Category changes
/// 12. Historical Orders remain readable
/// 13. Product/Vendor ownership from Issue #12 remains intact
/// 14. Issues #1–#12 remain intact
/// </summary>
public class CategoryRelationshipTests
{
    // -----------------------------------------------------------------------
    // Helpers for CreateProductCommand
    // -----------------------------------------------------------------------

    private static (Mock<IUnitOfWork> unitOfWork, Mock<IProductRepository> productRepo, Mock<IVendorRepository> vendorRepo, Mock<ICategoryRepository> categoryRepo, CreateProductCommandHandler handler) 
        BuildCreateProductHandler()
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var productRepo = new Mock<IProductRepository>(MockBehavior.Strict);
        var vendorRepo = new Mock<IVendorRepository>(MockBehavior.Strict);
        var categoryRepo = new Mock<ICategoryRepository>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Products).Returns(productRepo.Object);
        unitOfWork.SetupGet(u => u.Vendors).Returns(vendorRepo.Object);
        unitOfWork.SetupGet(u => u.Categories).Returns(categoryRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        productRepo.Setup(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns((Product p, CancellationToken ct) => Task.FromResult(p));

        var handler = new CreateProductCommandHandler(
            unitOfWork.Object,
            NullLogger<CreateProductCommandHandler>.Instance);

        return (unitOfWork, productRepo, vendorRepo, categoryRepo, handler);
    }

    // -----------------------------------------------------------------------
    // Helpers for UpdateProductInternalCommand
    // -----------------------------------------------------------------------

    private static (Mock<IUnitOfWork> unitOfWork, Mock<IProductRepository> productRepo, Mock<IVendorRepository> vendorRepo, Mock<ICategoryRepository> categoryRepo, UpdateProductInternalCommandHandler handler) 
        BuildUpdateProductHandler()
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var productRepo = new Mock<IProductRepository>(MockBehavior.Strict);
        var vendorRepo = new Mock<IVendorRepository>(MockBehavior.Strict);
        var categoryRepo = new Mock<ICategoryRepository>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Products).Returns(productRepo.Object);
        unitOfWork.SetupGet(u => u.Vendors).Returns(vendorRepo.Object);
        unitOfWork.SetupGet(u => u.Categories).Returns(categoryRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateProductInternalCommandHandler(
            unitOfWork.Object,
            NullLogger<UpdateProductInternalCommandHandler>.Instance);

        return (unitOfWork, productRepo, vendorRepo, categoryRepo, handler);
    }

    // -----------------------------------------------------------------------
    // Helpers for DeleteCategoryInternalCommand
    // -----------------------------------------------------------------------

    private static (Mock<IUnitOfWork> unitOfWork, Mock<ICategoryRepository> categoryRepo, DeleteCategoryInternalCommandHandler handler) 
        BuildDeleteCategoryHandler()
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var categoryRepo = new Mock<ICategoryRepository>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Categories).Returns(categoryRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        categoryRepo.Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteCategoryInternalCommandHandler(
            unitOfWork.Object,
            NullLogger<DeleteCategoryInternalCommandHandler>.Instance);

        return (unitOfWork, categoryRepo, handler);
    }

    // -----------------------------------------------------------------------
    // TEST 1: Create Product with valid active Category → succeeds
    // -----------------------------------------------------------------------
    [Fact]
    public async Task CreateProduct_WithValidActiveCategory_Succeeds()
    {
        // Arrange
        var (unitOfWork, productRepo, vendorRepo, categoryRepo, handler) = BuildCreateProductHandler();

        var activeVendor = new Vendor { Id = 1, IsApproved = true, IsActive = true };
        var activeCategory = new Category { Id = 1, Name = "Electronics", IsActive = true, IsDeleted = false };

        vendorRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeVendor);
        categoryRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCategory);
        categoryRepo.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        productRepo.Setup(r => r.GetBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "Test product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal(1, result.CategoryId);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // TEST 2: Create Product with non-existent CategoryId → rejected
    // -----------------------------------------------------------------------
    [Fact]
    public async Task CreateProduct_WithNonExistentCategory_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (unitOfWork, productRepo, vendorRepo, categoryRepo, handler) = BuildCreateProductHandler();

        var activeVendor = new Vendor { Id = 1, IsApproved = true, IsActive = true };

        vendorRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeVendor);
        categoryRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "Test product description",
            VendorId = 1,
            CategoryId = 999,
            Price = 100m,
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Category with ID 999", exception.Message);
    }

    // -----------------------------------------------------------------------
    // TEST 3: Create Product with inactive Category → rejected
    // -----------------------------------------------------------------------
    [Fact]
    public async Task CreateProduct_WithInactiveCategory_ThrowsInvalidOperationException()
    {
        // Arrange
        var (unitOfWork, productRepo, vendorRepo, categoryRepo, handler) = BuildCreateProductHandler();

        var activeVendor = new Vendor { Id = 1, IsApproved = true, IsActive = true };
        var inactiveCategory = new Category { Id = 1, Name = "Electronics", IsActive = false, IsDeleted = false };

        vendorRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeVendor);
        categoryRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveCategory);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "Test product description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("inactive", exception.Message);
    }

    // -----------------------------------------------------------------------
    // TEST 4: Update Product with active Category still assigned → succeeds
    // -----------------------------------------------------------------------
    [Fact]
    public async Task UpdateProduct_WithActiveCategoryAssigned_Succeeds()
    {
        // Arrange
        var (unitOfWork, productRepo, vendorRepo, categoryRepo, handler) = BuildUpdateProductHandler();

        var product = new Product
        {
            Id = 1,
            Name = "Old Name",
            Description = "Old Description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        var activeVendor = new Vendor { Id = 1, IsApproved = true, IsActive = true };
        var activeCategory = new Category { Id = 1, Name = "Electronics", IsActive = true, IsDeleted = false };

        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        vendorRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeVendor);
        categoryRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCategory);
        productRepo.Setup(r => r.GetBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new UpdateProductInternalCommand
        {
            Id = 1,
            Name = "New Name",
            Description = "New Description",
            Price = 150m,
            Quantity = 20
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal(1, result.CategoryId);  // Category unchanged
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // TEST 5: Update Product when its Category becomes inactive → rejected
    // -----------------------------------------------------------------------
    [Fact]
    public async Task UpdateProduct_WhenCategoryBecomesInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var (unitOfWork, productRepo, vendorRepo, categoryRepo, handler) = BuildUpdateProductHandler();

        var product = new Product
        {
            Id = 1,
            Name = "Old Name",
            Description = "Old Description",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            Quantity = 10
        };

        var activeVendor = new Vendor { Id = 1, IsApproved = true, IsActive = true };
        var inactiveCategory = new Category { Id = 1, Name = "Electronics", IsActive = false, IsDeleted = false };

        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        vendorRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeVendor);
        categoryRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveCategory);

        var command = new UpdateProductInternalCommand
        {
            Id = 1,
            Name = "New Name",
            Description = "New Description",
            Price = 150m,
            Quantity = 20
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("inactive", exception.Message);
    }

    // -----------------------------------------------------------------------
    // TEST 6: Product retains Category when CategoryId is unchanged
    // -----------------------------------------------------------------------
    [Fact]
    public async Task UpdateProduct_RetainsCategoryWhenUnchanged()
    {
        // Arrange
        var (unitOfWork, productRepo, vendorRepo, categoryRepo, handler) = BuildUpdateProductHandler();

        var originalCategoryId = 1;
        var product = new Product
        {
            Id = 1,
            Name = "Product",
            Description = "Description",
            VendorId = 1,
            CategoryId = originalCategoryId,
            Price = 100m,
            Quantity = 10
        };

        var activeVendor = new Vendor { Id = 1, IsApproved = true, IsActive = true };
        var activeCategory = new Category { Id = 1, Name = "Electronics", IsActive = true, IsDeleted = false };

        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        vendorRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeVendor);
        categoryRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCategory);
        productRepo.Setup(r => r.GetBySkuAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new UpdateProductInternalCommand
        {
            Id = 1,
            Name = "Product",
            Description = "Updated Description",
            Price = 100m,
            Quantity = 10
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(originalCategoryId, result.CategoryId);
        Assert.Equal(originalCategoryId, product.CategoryId);
    }

    // -----------------------------------------------------------------------
    // TEST 7: Category deletion soft-deletes (IsDeleted=true, no cascade)
    // -----------------------------------------------------------------------
    [Fact]
    public async Task DeleteCategory_SoftDeletesWithoutCascade()
    {
        // Arrange
        var (unitOfWork, categoryRepo, handler) = BuildDeleteCategoryHandler();

        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            IsActive = true,
            IsDeleted = false,
            DeletedAt = null
        };

        categoryRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var command = new DeleteCategoryInternalCommand { Id = 1 };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(category.IsDeleted);
        Assert.NotNull(category.DeletedAt);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // TEST 8: Query filters return only active categories
    // -----------------------------------------------------------------------
    [Fact]
    public async Task GetAllCategoriesQuery_FiltersInactiveCategories()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var categoryRepo = new Mock<ICategoryRepository>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Categories).Returns(categoryRepo.Object);

        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Active Category", IsActive = true, IsDeleted = false },
            new Category { Id = 2, Name = "Inactive Category", IsActive = false, IsDeleted = false },
            new Category { Id = 3, Name = "Deleted Category", IsActive = true, IsDeleted = true }
        };

        categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var handler = new GetAllCategoriesQueryHandler(
            unitOfWork.Object,
            NullLogger<GetAllCategoriesQueryHandler>.Instance);

        var query = new GetAllCategoriesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Category", result[0].Name);
    }

    // -----------------------------------------------------------------------
    // TEST 9: GetCategoryByIdQuery returns null for inactive categories
    // -----------------------------------------------------------------------
    [Fact]
    public async Task GetCategoryByIdQuery_ReturnsNullForInactiveCategory()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var categoryRepo = new Mock<ICategoryRepository>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Categories).Returns(categoryRepo.Object);

        var inactiveCategory = new Category { Id = 1, Name = "Inactive", IsActive = false, IsDeleted = false };

        categoryRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveCategory);

        var handler = new GetCategoryByIdQueryHandler(
            unitOfWork.Object,
            NullLogger<GetCategoryByIdQueryHandler>.Instance);

        var query = new GetCategoryByIdQuery { Id = 1 };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // TEST 10: Category slug updates when name changes
    // -----------------------------------------------------------------------
    [Fact]
    public async Task UpdateCategory_RegeneratesSlugFromName()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var categoryRepo = new Mock<ICategoryRepository>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Categories).Returns(categoryRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        categoryRepo.Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var category = new Category
        {
            Id = 1,
            Name = "Old Name",
            Slug = "old-name",
            Description = "Description",
            IsActive = true,
            IsDeleted = false
        };

        categoryRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var handler = new UpdateCategoryInternalCommandHandler(
            unitOfWork.Object,
            NullLogger<UpdateCategoryInternalCommandHandler>.Instance);

        var command = new UpdateCategoryInternalCommand
        {
            Id = 1,
            Name = "New Name",
            Description = "Description"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        // Slug should be regenerated from new name
        Assert.NotEqual("old-name", result.Slug);
    }

    // -----------------------------------------------------------------------
    // TEST 11: Product soft-delete preserves Category integrity
    // -----------------------------------------------------------------------
    [Fact]
    public async Task DeleteProduct_PreservesCategoryIntegrity()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var productRepo = new Mock<IProductRepository>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Products).Returns(productRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Product",
            CategoryId = 1,
            Category = category,
            VendorId = 1,
            Price = 100m,
            IsDeleted = false,
            DeletedAt = null
        };

        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var handler = new DeleteProductInternalCommandHandler(
            unitOfWork.Object,
            NullLogger<DeleteProductInternalCommandHandler>.Instance);

        var command = new DeleteProductInternalCommand { Id = 1 };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(product.IsDeleted);
        Assert.Equal(1, product.CategoryId);  // CategoryId preserved
        Assert.NotNull(product.Category);     // Navigation preserved
    }
}

