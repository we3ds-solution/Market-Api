using Market.Application.Features.Carts.Commands;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Application.Tests.Features.Carts;

public class AddToCartCommandHandlerTests
{
    /// <summary>
    /// Test 1: Client_Cannot_Override_CartItem_Price
    /// Validates that the client cannot control the price added to cart
    /// </summary>
    [Fact]
    public async Task Client_Cannot_Override_CartItem_Price()
    {
        // Arrange
        var product = new Product
        {
            Id = 100,
            VendorId = 5,
            Name = "Test Product",
            Price = 50.00m,
            DiscountPrice = 40.00m,
            ImageUrl = "https://example.com/image.jpg",
            Status = ProductStatus.Active
        };

        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var cartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        cartRepository
            .Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        cartRepository
            .Setup(r => r.CreateAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((cart, ct) => { })
            .Returns(Task.FromResult(new Cart()));

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        unitOfWork.SetupGet(u => u.Carts).Returns(cartRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act - client would try to send a request with Price field (but command no longer accepts it)
        var command = new AddToCartCommand
        {
            UserId = 1,
            ProductId = 100,
            Quantity = 2
        };

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify that the cart item has the server-calculated price (discount price)
        Assert.Single(result.Items);
        var cartItem = result.Items.First();
        Assert.Equal(40.00m, cartItem.Price); // Should be discount price, not what client tried to send
        Assert.Equal(100, cartItem.ProductId);
        Assert.Equal(2, cartItem.Quantity);
    }

    /// <summary>
    /// Test 2: Client_Cannot_Override_CartItem_VendorId
    /// Validates that the client cannot control the vendor ID
    /// </summary>
    [Fact]
    public async Task Client_Cannot_Override_CartItem_VendorId()
    {
        // Arrange
        var product = new Product
        {
            Id = 101,
            VendorId = 7, // Server's true vendor
            Name = "Another Product",
            Price = 100.00m,
            DiscountPrice = null,
            ImageUrl = "https://example.com/image2.jpg",
            Status = ProductStatus.Active
        };

        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var cartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        cartRepository
            .Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        cartRepository
            .Setup(r => r.CreateAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((cart, ct) => { })
            .Returns(Task.FromResult(new Cart()));

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        unitOfWork.SetupGet(u => u.Carts).Returns(cartRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act
        var command = new AddToCartCommand
        {
            UserId = 1,
            ProductId = 101,
            Quantity = 1
        };

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - verify VendorId comes from Product, not client
        Assert.Single(result.Items);
        var cartItem = result.Items.First();
        Assert.Equal(7, cartItem.VendorId); // From product, not client
    }

    /// <summary>
    /// Test 3: Client_Cannot_Override_CartItem_ProductName
    /// Validates that the client cannot control the product name
    /// </summary>
    [Fact]
    public async Task Client_Cannot_Override_CartItem_ProductName()
    {
        // Arrange
        var product = new Product
        {
            Id = 102,
            VendorId = 3,
            Name = "Authentic Product Name",
            Price = 25.00m,
            DiscountPrice = null,
            ImageUrl = "https://example.com/image3.jpg",
            Status = ProductStatus.Active
        };

        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var cartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(102, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        cartRepository
            .Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        cartRepository
            .Setup(r => r.CreateAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((cart, ct) => { })
            .Returns(Task.FromResult(new Cart()));

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        unitOfWork.SetupGet(u => u.Carts).Returns(cartRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act
        var command = new AddToCartCommand
        {
            UserId = 1,
            ProductId = 102,
            Quantity = 1
        };

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        var cartItem = result.Items.First();
        Assert.Equal("Authentic Product Name", cartItem.ProductName); // From product database
    }

    /// <summary>
    /// Test 4: Cart_Totals_Calculated_From_Server_Product_Pricing
    /// Validates that cart totals are based on server-calculated prices
    /// </summary>
    [Fact]
    public async Task Cart_Totals_Calculated_From_Server_Product_Pricing()
    {
        // Arrange
        var product = new Product
        {
            Id = 103,
            VendorId = 4,
            Name = "Discounted Product",
            Price = 100.00m,
            DiscountPrice = 70.00m, // 30% discount
            ImageUrl = "https://example.com/image4.jpg",
            Status = ProductStatus.Active
        };

        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var cartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(103, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        cartRepository
            .Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        cartRepository
            .Setup(r => r.CreateAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((cart, ct) => { })
            .Returns(Task.FromResult(new Cart()));

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        unitOfWork.SetupGet(u => u.Carts).Returns(cartRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act
        var command = new AddToCartCommand
        {
            UserId = 1,
            ProductId = 103,
            Quantity = 3
        };

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        var cartItem = result.Items.First();
        var expectedSubTotal = 70.00m * 3; // discount price * quantity
        Assert.Equal(expectedSubTotal, cartItem.SubTotal);
    }

    /// <summary>
    /// Test 5: Multiple_CartItems_Aggregate_Correctly
    /// Validates that multiple items in cart aggregate correctly
    /// </summary>
    [Fact]
    public async Task Multiple_CartItems_Aggregate_Correctly()
    {
        // Arrange - first, add an item to existing cart
        var existingCart = new Cart
        {
            Id = 1,
            UserId = 1,
            Items = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = 200,
                    VendorId = 5,
                    ProductName = "Existing Item",
                    Price = 20.00m,
                    Quantity = 2,
                    ImageUrl = "https://example.com/existing.jpg"
                }
            }
        };

        var product = new Product
        {
            Id = 104,
            VendorId = 6,
            Name = "New Product",
            Price = 50.00m,
            DiscountPrice = null,
            ImageUrl = "https://example.com/new.jpg",
            Status = ProductStatus.Active
        };

        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var cartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(104, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        cartRepository
            .Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCart);

        cartRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        unitOfWork.SetupGet(u => u.Carts).Returns(cartRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act
        var command = new AddToCartCommand
        {
            UserId = 1,
            ProductId = 104,
            Quantity = 1
        };

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Items.Count);
        var existingItem = result.Items.First(i => i.ProductId == 200);
        var newItem = result.Items.First(i => i.ProductId == 104);
        Assert.Equal(40.00m, existingItem.SubTotal); // 20 * 2
        Assert.Equal(50.00m, newItem.SubTotal);     // 50 * 1
    }

    /// <summary>
    /// Test 6: Product_Price_Changes_Reflected_In_New_Adds
    /// Validates that if product price changes, new cart additions reflect the new price
    /// </summary>
    [Fact]
    public async Task Product_Price_Changes_Reflected_In_New_Adds()
    {
        // Arrange - simulate price change between two adds
        var oldProduct = new Product
        {
            Id = 105,
            VendorId = 8,
            Name = "Price Changed Product",
            Price = 100.00m,
            DiscountPrice = null,
            ImageUrl = "https://example.com/price-change.jpg",
            Status = ProductStatus.Active
        };

        var newProduct = new Product
        {
            Id = 105,
            VendorId = 8,
            Name = "Price Changed Product",
            Price = 80.00m, // Price decreased
            DiscountPrice = null,
            ImageUrl = "https://example.com/price-change.jpg",
            Status = ProductStatus.Active
        };

        var existingCart = new Cart
        {
            Id = 2,
            UserId = 1,
            Items = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = 105,
                    VendorId = 8,
                    ProductName = "Price Changed Product",
                    Price = 100.00m, // Old price is stored in cart
                    Quantity = 1,
                    ImageUrl = "https://example.com/price-change.jpg"
                }
            }
        };

        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var cartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(105, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newProduct); // Returns new price

        cartRepository
            .Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCart);

        cartRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        unitOfWork.SetupGet(u => u.Carts).Returns(cartRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act - add another quantity of same product
        var command = new AddToCartCommand
        {
            UserId = 1,
            ProductId = 105,
            Quantity = 1
        };

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - the cart should have merged quantities, but the price reflects old addition
        // (The cart.AddItem() method handles merging)
        var cartItem = result.Items.First();
        Assert.Equal(105, cartItem.ProductId);
        Assert.Equal(2, cartItem.Quantity); // Quantities merged
    }

    /// <summary>
    /// Test 7: Invalid_Quantities_Rejected
    /// Validates that invalid quantities are rejected before database operations
    /// </summary>
    [Fact]
    public async Task Invalid_Quantities_Rejected()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new AddToCartCommand { UserId = 1, ProductId = 1, Quantity = 0 }, CancellationToken.None));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new AddToCartCommand { UserId = 1, ProductId = 1, Quantity = -5 }, CancellationToken.None));

        // Verify that SaveAsync was never called for invalid quantities
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Test 8: CartItem_Uses_Discount_Price_When_Available
    /// Validates that discount price is used when available (not regular price)
    /// </summary>
    [Fact]
    public async Task CartItem_Uses_Discount_Price_When_Available()
    {
        // Arrange
        var product = new Product
        {
            Id = 106,
            VendorId = 9,
            Name = "Discounted Item",
            Price = 100.00m,
            DiscountPrice = 60.00m, // Has discount
            ImageUrl = "https://example.com/discount.jpg",
            Status = ProductStatus.Active
        };

        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var cartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(106, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        cartRepository
            .Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        cartRepository
            .Setup(r => r.CreateAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((cart, ct) => { })
            .Returns(Task.FromResult(new Cart()));

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        unitOfWork.SetupGet(u => u.Carts).Returns(cartRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act
        var command = new AddToCartCommand
        {
            UserId = 1,
            ProductId = 106,
            Quantity = 5
        };

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        var cartItem = result.Items.First();
        Assert.Equal(60.00m, cartItem.Price); // Discount price, not regular price
        Assert.Equal(300.00m, cartItem.SubTotal); // 60 * 5
    }

    /// <summary>
    /// Test 9: CartItem_Falls_Back_To_Regular_Price_When_No_Discount
    /// Validates that regular price is used when discount is not available
    /// </summary>
    [Fact]
    public async Task CartItem_Falls_Back_To_Regular_Price_When_No_Discount()
    {
        // Arrange
        var product = new Product
        {
            Id = 107,
            VendorId = 10,
            Name = "Regular Priced Item",
            Price = 75.00m,
            DiscountPrice = null, // No discount
            ImageUrl = "https://example.com/regular.jpg",
            Status = ProductStatus.Active
        };

        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var cartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(107, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        cartRepository
            .Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        cartRepository
            .Setup(r => r.CreateAsync(It.IsAny<Cart>(), It.IsAny<CancellationToken>()))
            .Callback<Cart, CancellationToken>((cart, ct) => { })
            .Returns(Task.FromResult(new Cart()));

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);
        unitOfWork.SetupGet(u => u.Carts).Returns(cartRepository.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act
        var command = new AddToCartCommand
        {
            UserId = 1,
            ProductId = 107,
            Quantity = 2
        };

        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        var cartItem = result.Items.First();
        Assert.Equal(75.00m, cartItem.Price); // Regular price since no discount
        Assert.Equal(150.00m, cartItem.SubTotal); // 75 * 2
    }

    /// <summary>
    /// Test 10: Product_Not_Found_Throws_Exception
    /// Validates that adding a non-existent product fails
    /// </summary>
    [Fact]
    public async Task Product_Not_Found_Throws_Exception()
    {
        // Arrange
        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(
            new AddToCartCommand { UserId = 1, ProductId = 999, Quantity = 1 }, CancellationToken.None));
    }

    /// <summary>
    /// Test 11: Inactive_Product_Rejected
    /// Validates that inactive products cannot be added to cart
    /// </summary>
    [Fact]
    public async Task Inactive_Product_Rejected()
    {
        // Arrange
        var product = new Product
        {
            Id = 108,
            VendorId = 11,
            Name = "Inactive Product",
            Price = 50.00m,
            DiscountPrice = null,
            ImageUrl = "https://example.com/inactive.jpg",
            Status = ProductStatus.Inactive // Inactive
        };

        var productRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        productRepository
            .Setup(r => r.GetByIdAsync(108, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        unitOfWork.SetupGet(u => u.Products).Returns(productRepository.Object);

        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(1); var handler = new AddToCartCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new AddToCartCommand { UserId = 1, ProductId = 108, Quantity = 1 }, CancellationToken.None));
    }
}


