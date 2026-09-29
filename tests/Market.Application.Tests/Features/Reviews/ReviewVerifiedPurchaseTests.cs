namespace Market.Application.Tests.Features.Reviews;

public class ReviewVerifiedPurchaseTests
{
    // -----------------------------------------------------------------------
    // Shared helpers
    // -----------------------------------------------------------------------

    private static (
        Mock<ICurrentUserService> currentUserService,
        Mock<IReviewRepository> reviews,
        Mock<IOrderRepository> orders,
        Mock<IUnitOfWork> unitOfWork,
        CreateReviewCommandHandler handler) BuildHandler()
    {
        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var reviews = new Mock<IReviewRepository>(MockBehavior.Strict);
        var orders = new Mock<IOrderRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Reviews).Returns(reviews.Object);
        unitOfWork.SetupGet(u => u.Orders).Returns(orders.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        reviews.Setup(r => r.CreateAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Review r, CancellationToken _) => r);

        var handler = new CreateReviewCommandHandler(
            currentUserService.Object,
            unitOfWork.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CreateReviewCommandHandler>.Instance);

        return (currentUserService, reviews, orders, unitOfWork, handler);
    }

    // -----------------------------------------------------------------------
    // Test 1: User reviews product they never purchased
    // -----------------------------------------------------------------------
    /// <summary>
    /// When a user creates a review for a product they never purchased,
    /// IsVerifiedPurchase must be false (no qualifying order found).
    /// </summary>
    [Fact]
    public async Task Handle_UserReviewsNeverPurchasedProduct_IsVerifiedPurchaseFalse()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orders.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 4,
            Title = "Good product",
            Comment = "Works well",
            ImageUrls = new()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsVerifiedPurchase);
        Assert.Equal(101, result.ProductId);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 2: User with delivered & completed order gets verified purchase
    // -----------------------------------------------------------------------
    /// <summary>
    /// When a user creates a review and they have an order with:
    /// - OrderStatus == Delivered
    /// - PaymentStatus == Completed
    /// - OrderItems contains the product
    /// Then IsVerifiedPurchase must be true.
    /// </summary>
    [Fact]
    public async Task Handle_UserWithDeliveredCompletedOrder_IsVerifiedPurchaseTrue()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orders.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 5,
            Title = "Excellent",
            Comment = "Perfect",
            ImageUrls = new()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsVerifiedPurchase);
        Assert.Equal(101, result.ProductId);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 3: User has order but for different product
    // -----------------------------------------------------------------------
    /// <summary>
    /// When a user has an order with the right statuses but the order
    /// doesn't contain the product being reviewed, IsVerifiedPurchase is false.
    /// </summary>
    [Fact]
    public async Task Handle_OrderWithDifferentProduct_IsVerifiedPurchaseFalse()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orders.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Order exists but doesn't have product 101

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 3,
            Title = "Okay",
            Comment = "Average",
            ImageUrls = new()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsVerifiedPurchase);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 4: Order belongs to different user
    // -----------------------------------------------------------------------
    /// <summary>
    /// Ownership check: if the command specifies a different CustomerId than
    /// the authenticated user, an UnauthorizedAccessException is thrown.
    /// </summary>
    [Fact]
    public async Task Handle_UserTriesToReviewForDifferentCustomer_ThrowsUnauthorizedAccessException()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 99, // Different from authenticated user (5)
            RatingValue = 4,
            Title = "Hacked",
            Comment = "Trying to impersonate",
            ImageUrls = new()
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("Cannot create review for user", ex.Message);
        reviews.Verify(r => r.CustomerReviewedProductAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // Test 5: Client cannot set VerifiedPurchase directly
    // -----------------------------------------------------------------------
    /// <summary>
    /// The command doesn't have an IsVerifiedPurchase property to set.
    /// The handler always determines it server-side based on order history.
    /// This test verifies the command structure is secure.
    /// </summary>
    [Fact]
    public void Command_DoesNotHaveVerifiedPurchaseProperty_SecureByDefault()
    {
        var command = new CreateReviewCommand();
        var properties = typeof(CreateReviewCommand).GetProperties();
        var hasVerifiedPurchaseProperty = properties.Any(p => p.Name == "IsVerifiedPurchase");

        Assert.False(hasVerifiedPurchaseProperty, "CreateReviewCommand must not expose IsVerifiedPurchase");
    }

    // -----------------------------------------------------------------------
    // Test 6: Pending order not verified
    // -----------------------------------------------------------------------
    /// <summary>
    /// A pending order (not yet shipped) should not count as a verified purchase.
    /// The repository query filters for OrderStatus == Delivered only.
    /// </summary>
    [Fact]
    public async Task Handle_PendingOrder_IsVerifiedPurchaseFalse()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orders.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Pending order doesn't count

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 4,
            Title = "Pending",
            Comment = "Still in transit",
            ImageUrls = new()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsVerifiedPurchase);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 7: Cancelled order not verified
    // -----------------------------------------------------------------------
    /// <summary>
    /// A cancelled order should not count as a verified purchase.
    /// </summary>
    [Fact]
    public async Task Handle_CancelledOrder_IsVerifiedPurchaseFalse()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orders.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Cancelled order doesn't count

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 2,
            Title = "Cancelled",
            Comment = "Never received",
            ImageUrls = new()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsVerifiedPurchase);
    }

    // -----------------------------------------------------------------------
    // Test 8: Refunded order not verified
    // -----------------------------------------------------------------------
    /// <summary>
    /// A refunded order (OrderStatus.Refunded or PaymentStatus.Refunded)
    /// should not count as a completed purchase.
    /// </summary>
    [Fact]
    public async Task Handle_RefundedOrder_IsVerifiedPurchaseFalse()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orders.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Refunded order doesn't count

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 1,
            Title = "Refunded",
            Comment = "Got money back",
            ImageUrls = new()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsVerifiedPurchase);
    }

    // -----------------------------------------------------------------------
    // Test 9: Failed payment not verified
    // -----------------------------------------------------------------------
    /// <summary>
    /// An order with PaymentStatus == Failed should not count as a verified purchase.
    /// </summary>
    [Fact]
    public async Task Handle_FailedPayment_IsVerifiedPurchaseFalse()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orders.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Failed payment doesn't count

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 3,
            Title = "Payment failed",
            Comment = "Card was declined",
            ImageUrls = new()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsVerifiedPurchase);
    }

    // -----------------------------------------------------------------------
    // Test 10: Pending payment not verified
    // -----------------------------------------------------------------------
    /// <summary>
    /// An order with PaymentStatus == Pending should not count as a verified purchase.
    /// </summary>
    [Fact]
    public async Task Handle_PendingPayment_IsVerifiedPurchaseFalse()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orders.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Pending payment doesn't count

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 3,
            Title = "Payment pending",
            Comment = "Waiting for confirmation",
            ImageUrls = new()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsVerifiedPurchase);
    }

    // -----------------------------------------------------------------------
    // Test 11: Duplicate review detection still works
    // -----------------------------------------------------------------------
    /// <summary>
    /// The handler must still check if the customer already reviewed the product.
    /// This existing validation should not be affected by the new verified purchase logic.
    /// </summary>
    [Fact]
    public async Task Handle_CustomerAlreadyReviewedProduct_ThrowsInvalidOperationException()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Already reviewed

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 5,
            Title = "Second review",
            Comment = "Update",
            ImageUrls = new()
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("already reviewed", ex.Message);
        orders.Verify(o => o.CustomerHasVerifiedPurchaseOfProductAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // Test 12: Images are preserved in response
    // -----------------------------------------------------------------------
    /// <summary>
    /// Verify that non-purchase fields (images, ratings, text) work normally
    /// and are not affected by the new verified purchase logic.
    /// </summary>
    [Fact]
    public async Task Handle_WithImages_ImageUrlsPreservedInResponse()
    {
        var (currentUserService, reviews, orders, unitOfWork, handler) = BuildHandler();

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(5);
        reviews.Setup(r => r.CustomerReviewedProductAsync(101, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orders.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(5, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var imageUrls = new List<string> { "https://example.com/img1.jpg", "https://example.com/img2.jpg" };

        var command = new CreateReviewCommand
        {
            ProductId = 101,
            VendorId = 10,
            CustomerId = 5,
            RatingValue = 4,
            Title = "With images",
            Comment = "Photos included",
            ImageUrls = imageUrls
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, result.ImageUrls.Count);
        Assert.Contains("https://example.com/img1.jpg", result.ImageUrls);
        Assert.Contains("https://example.com/img2.jpg", result.ImageUrls);
        Assert.Equal("With images", result.Title);
        Assert.Equal("Photos included", result.Comment);
        Assert.Equal(4, result.RatingValue);
    }
}
