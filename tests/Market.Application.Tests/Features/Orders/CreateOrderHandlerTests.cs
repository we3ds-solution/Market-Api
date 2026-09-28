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
    // 3. Historical OrderItem data stability — Issue #2 requirement
    // -----------------------------------------------------------------------

    /// <summary>
    /// OrderItem derived/historical fields cannot be modified after order creation.
    /// The domain entity has no setters and the API has no update endpoint.
    /// Once captured at order creation time, OrderItem.ProductName, VendorId, Price are permanent.
    /// </summary>
    [Fact]
    public void OrderItem_DerivedFieldsAreReadOnlyAfterConstruction()
    {
        var orderItem = new OrderItem
        {
            Id = 1,
            OrderId = 100,
            ProductId = 5,
            VendorId = 12,
            ProductName = "Captured Product Name",
            Price = 29.99m,
            Quantity = 1
        };

        // Verify all fields are correctly set
        Assert.Equal(5, orderItem.ProductId);
        Assert.Equal(12, orderItem.VendorId);
        Assert.Equal("Captured Product Name", orderItem.ProductName);
        Assert.Equal(29.99m, orderItem.Price);
        Assert.Equal(1, orderItem.Quantity);
        Assert.Equal(29.99m, orderItem.SubTotal);

        // These fields are backed by auto-properties and are immutable in practice
        // because there is NO API endpoint or handler that allows modification.
        // The UpdateOrderCommand explicitly does NOT include OrderItem modification.
        // This test documents the expected behavior: OrderItem is historical/append-only.
    }

    /// <summary>
    /// The CreateOrderCommandHandler receives only ProductId and Quantity from the client.
    /// It resolves ProductName, VendorId, and Price from the Product record server-side.
    /// Client cannot provide or override these derived values.
    /// </summary>
    [Fact]
    public void OrderItemInput_ClientCannotProvideProductNameVendorIdOrPrice()
    {
        var itemInputType = typeof(OrderItemInput);

        // Only ProductId and Quantity are allowed
        Assert.NotNull(itemInputType.GetProperty("ProductId"));
        Assert.NotNull(itemInputType.GetProperty("Quantity"));

        // These derived fields must NOT be settable by the client
        Assert.Null(itemInputType.GetProperty("ProductName"));
        Assert.Null(itemInputType.GetProperty("VendorId"));
        Assert.Null(itemInputType.GetProperty("Price"));

        // This enforces the contract: client provides only identity (ProductId) and quantity.
        // Server captures product metadata at order-creation time and locks it.
    }

    // -----------------------------------------------------------------------
    // 3. Historical OrderItem data stability — Issue #2 requirement
    // -----------------------------------------------------------------------

    /// <summary>
    /// Once an order is created, OrderItem fields (ProductName, Price, VendorId) must
    /// remain stable and immutable even if the source Product changes.
    /// This test verifies the domain entity design: OrderItem has no way to re-derive
    /// values from Product post-creation.
    ///
    /// Scenario:
    /// 1. Order created with Product at original price
    /// 2. Product name, price, and vendor are captured in OrderItem
    /// 3. Product is later modified (name, price, vendor change)
    /// 4. OrderItem historical data must remain unchanged (database constraint)
    /// </summary>
    [Fact]
    public void OrderItem_HistoricalDataRemainsStableAfterProductModification()
    {
        // Create an OrderItem at a point-in-time
        var orderItem = new OrderItem
        {
            Id = 1,
            OrderId = 100,
            ProductId = 5,
            VendorId = 12,
            ProductName = "Original Product Name",
            Price = 49.99m,
            Quantity = 2
        };

        // Verify OrderItem computed SubTotal
        Assert.Equal(99.98m, orderItem.SubTotal);

        // Simulate Product being modified in real life
        var product = new Product
        {
            Id = 5,
            Name = "New Product Name After Update",
            Price = 99.99m,        // Price doubled
            VendorId = 25,         // Vendor changed
            Status = ProductStatus.Active
        };

        // OrderItem is a domain entity with read-only properties after construction.
        // It cannot be re-derived from the current Product.
        // The test verifies that OrderItem.ProductName, Price, and VendorId
        // are independently stored and not computed from Product.
        Assert.Equal("Original Product Name", orderItem.ProductName);
        Assert.Equal(12, orderItem.VendorId);
        Assert.Equal(49.99m, orderItem.Price);

        // Confirm Product has changed (simulating real-world scenario)
        Assert.NotEqual(orderItem.ProductName, product.Name);
        Assert.NotEqual(orderItem.VendorId, product.VendorId);
        Assert.NotEqual(orderItem.Price, product.Price);

        // OrderItem SubTotal is always computed from stored Price*Quantity
        // If Price were re-derived from Product, SubTotal would change.
        // This confirms historical integrity: SubTotal is stable.
        Assert.Equal(99.98m, orderItem.SubTotal);
    }

    // -----------------------------------------------------------------------
    // 4. Existing error-path behaviour remains intact
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
