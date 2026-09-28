using Market.Application.Features.Orders.Commands;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Application.Tests.Features.Orders;

public class CreateOrderHandlerTests
{
    // -----------------------------------------------------------------------
    // Shared helpers
    // -----------------------------------------------------------------------

    private static (
        Mock<IUserRepository> users,
        Mock<IProductRepository> products,
        Mock<IOrderRepository> orders,
        Mock<IUnitOfWork> unitOfWork,
        CreateOrderCommandHandler handler) BuildHandler()
    {
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var orders = new Mock<IOrderRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Users).Returns(users.Object);
        unitOfWork.SetupGet(u => u.Products).Returns(products.Object);
        unitOfWork.SetupGet(u => u.Orders).Returns(orders.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        orders.Setup(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        var handler = new CreateOrderCommandHandler(unitOfWork.Object, NullLogger<CreateOrderCommandHandler>.Instance);
        return (users, products, orders, unitOfWork, handler);
    }

    // -----------------------------------------------------------------------
    // 1. Valid order — server-side pricing from persisted product data
    // -----------------------------------------------------------------------

    /// <summary>
    /// A valid order must have all financial values derived from the server-side product
    /// record. Inventory must be decremented and the response must reflect calculated values.
    ///
    /// Product: Price=25, DiscountPrice=20  Qty=2
    ///   SubTotal    = 20 * 2          = 40.00
    ///   Tax         = 40 * 0.08       =  3.20
    ///   ShippingCost (flat)           =  5.00
    ///   TotalPrice  = 40 + 3.20 + 5  = 48.20
    /// </summary>
    [Fact]
    public async Task Handle_ValidOrder_CalculatesPricingFromServerSideProductData()
    {
        var product = new Product
        {
            Id = 10,
            Name = "Persisted product",
            VendorId = 7,
            Price = 25m,
            DiscountPrice = 20m,
            Quantity = 5,
            Sold = 2,
            Status = ProductStatus.Active
        };

        var (users, products, _, unitOfWork, handler) = BuildHandler();
        users.Setup(r => r.ExistsAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        products.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        products.Setup(r => r.UpdateAsync(product, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new CreateOrderCommand
        {
            CustomerId = 3,
            ShippingAddress = "123 Market Street",
            Items = [new OrderItemInput { ProductId = 10, Quantity = 2 }]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        // Inventory updated
        Assert.Equal(3, product.Quantity);
        Assert.Equal(4, product.Sold);

        // Item fields resolved from the product record, not from any client input
        var item = Assert.Single(result.Items);
        Assert.Equal("Persisted product", item.ProductName);
        Assert.Equal(7, item.VendorId);
        Assert.Equal(20m, item.Price);           // DiscountPrice wins over Price
        Assert.Equal(40m, item.SubTotal);

        // Order-level financial values are server-calculated
        Assert.Equal(40.00m, result.SubTotal);
        Assert.Equal(3.20m, result.Tax);         // 40 * 8%
        Assert.Equal(5.00m, result.ShippingCost);
        Assert.Equal(48.20m, result.TotalPrice); // 40 + 3.20 + 5.00

        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// When no DiscountPrice is set the unit price must fall back to Product.Price.
    ///
    /// Product: Price=30, DiscountPrice=null  Qty=3
    ///   SubTotal    = 30 * 3          = 90.00
    ///   Tax         = 90 * 0.08       =  7.20
    ///   ShippingCost (flat)           =  5.00
    ///   TotalPrice  = 90 + 7.20 + 5  = 102.20
    /// </summary>
    [Fact]
    public async Task Handle_ValidOrder_WhenNoDiscountPrice_UsesRegularProductPrice()
    {
        var product = new Product
        {
            Id = 20,
            Name = "Full Price Product",
            VendorId = 3,
            Price = 30m,
            DiscountPrice = null,
            Quantity = 10,
            Sold = 0,
            Status = ProductStatus.Active
        };

        var (users, products, _, unitOfWork, handler) = BuildHandler();
        users.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        products.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        products.Setup(r => r.UpdateAsync(product, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await handler.Handle(new CreateOrderCommand
        {
            CustomerId = 1,
            ShippingAddress = "456 Commerce Ave, Suite 10",
            Items = [new OrderItemInput { ProductId = 20, Quantity = 3 }]
        }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(30m, item.Price);
        Assert.Equal(90m, item.SubTotal);
        Assert.Equal(90.00m, result.SubTotal);
        Assert.Equal(7.20m, result.Tax);
        Assert.Equal(5.00m, result.ShippingCost);
        Assert.Equal(102.20m, result.TotalPrice);
    }

    // -----------------------------------------------------------------------
    // 2. Client-supplied financial values cannot override server calculations
    // -----------------------------------------------------------------------

    /// <summary>
    /// The CreateOrderCommand no longer exposes SubTotal, ShippingCost, Tax, or
    /// any per-item price/vendor/name fields. Verifies the command type does NOT
    /// have those properties so the compiler would reject any attempt to supply them.
    /// </summary>
    [Fact]
    public void CreateOrderCommand_DoesNotExposeClientControllableFinancialFields()
    {
        var commandType = typeof(CreateOrderCommand);
        var itemType = typeof(OrderItemInput);

        // These fields must NOT exist on the command
        Assert.Null(commandType.GetProperty("SubTotal"));
        Assert.Null(commandType.GetProperty("ShippingCost"));
        Assert.Null(commandType.GetProperty("Tax"));

        // These fields must NOT exist on the item input
        Assert.Null(itemType.GetProperty("Price"));
        Assert.Null(itemType.GetProperty("VendorId"));
        Assert.Null(itemType.GetProperty("ProductName"));
    }

    /// <summary>
    /// Even if a caller constructs an order with multiple items the tax and total
    /// are still calculated correctly server-side across all line items.
    ///
    /// Item A: Price=10, Qty=2 → 20
    /// Item B: Price=50, Qty=1 → 50
    ///   SubTotal    = 70.00
    ///   Tax         = 70 * 0.08       =  5.60
    ///   ShippingCost (flat)           =  5.00
    ///   TotalPrice  = 70 + 5.60 + 5  = 80.60
    /// </summary>
    [Fact]
    public async Task Handle_MultipleItems_ServerCalculatesAggregateFinancials()
    {
        var productA = new Product { Id = 1, Name = "Widget A", VendorId = 10, Price = 10m, Quantity = 5, Sold = 0, Status = ProductStatus.Active };
        var productB = new Product { Id = 2, Name = "Widget B", VendorId = 11, Price = 50m, Quantity = 3, Sold = 0, Status = ProductStatus.Active };

        var (users, products, _, unitOfWork, handler) = BuildHandler();
        users.Setup(r => r.ExistsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        products.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(productA);
        products.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(productB);
        products.Setup(r => r.UpdateAsync(productA, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        products.Setup(r => r.UpdateAsync(productB, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await handler.Handle(new CreateOrderCommand
        {
            CustomerId = 5,
            ShippingAddress = "789 Marketplace Blvd, Floor 2",
            Items =
            [
                new OrderItemInput { ProductId = 1, Quantity = 2 },
                new OrderItemInput { ProductId = 2, Quantity = 1 }
            ]
        }, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(70.00m, result.SubTotal);
        Assert.Equal(5.60m, result.Tax);
        Assert.Equal(5.00m, result.ShippingCost);
        Assert.Equal(80.60m, result.TotalPrice);
    }

    // -----------------------------------------------------------------------
    // 3. Existing error-path behaviour remains intact
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Handle_WhenProductDoesNotHaveSufficientStock_DoesNotPersistOrder()
    {
        var (users, products, _, unitOfWork, handler) = BuildHandler();
        users.Setup(r => r.ExistsAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        products.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = 10, Quantity = 1, Status = ProductStatus.Active });

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new CreateOrderCommand
        {
            CustomerId = 3,
            ShippingAddress = "123 Market Street",
            Items = [new OrderItemInput { ProductId = 10, Quantity = 2 }]
        }, CancellationToken.None));

        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_DoesNotPersistOrder()
    {
        var (users, products, _, unitOfWork, handler) = BuildHandler();
        users.Setup(r => r.ExistsAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        products.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(new CreateOrderCommand
        {
            CustomerId = 3,
            ShippingAddress = "123 Market Street",
            Items = [new OrderItemInput { ProductId = 10, Quantity = 1 }]
        }, CancellationToken.None));

        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
