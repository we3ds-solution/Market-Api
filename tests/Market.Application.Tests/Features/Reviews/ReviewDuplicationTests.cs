using Market.Application.Features.Reviews.Commands;
using Market.Domain.Entities;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Market.Application.Tests.Features.Reviews;

/// <summary>
/// Issue #14: Review Duplication Prevention - Comprehensive Tests
/// </summary>
public class ReviewDuplicationTests
{
    [Fact]
    public async Task CreateReview_FirstReviewForProduct_Succeeds()
    {
        // Arrange
        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var reviewRepo = new Mock<IReviewRepository>(MockBehavior.Strict);
        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Reviews).Returns(reviewRepo.Object);
        unitOfWork.SetupGet(u => u.Orders).Returns(orderRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(10);
        reviewRepo.Setup(r => r.CustomerReviewedProductAsync(101, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orderRepo.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(10, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var review = new Review { ProductId = 101, CustomerId = 10, RatingValue = 5 };
        reviewRepo.Setup(r => r.CreateAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var handler = new CreateReviewCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<CreateReviewCommandHandler>.Instance);
        var command = new CreateReviewCommand { ProductId = 101, VendorId = 20, CustomerId = 10, RatingValue = 5, Title = "Test", Comment = "Good", ImageUrls = new() };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(101, result.ProductId);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReview_DuplicateReview_ThrowsInvalidOperationException()
    {
        // Arrange
        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var reviewRepo = new Mock<IReviewRepository>(MockBehavior.Strict);
        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Reviews).Returns(reviewRepo.Object);
        unitOfWork.SetupGet(u => u.Orders).Returns(orderRepo.Object);

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(10);
        reviewRepo.Setup(r => r.CustomerReviewedProductAsync(101, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);  // Already reviewed

        var handler = new CreateReviewCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<CreateReviewCommandHandler>.Instance);
        var command = new CreateReviewCommand { ProductId = 101, VendorId = 20, CustomerId = 10, RatingValue = 5, Title = "Test", Comment = "Good", ImageUrls = new() };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateReview_DifferentProduct_Succeeds()
    {
        // Arrange
        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var reviewRepo = new Mock<IReviewRepository>(MockBehavior.Strict);
        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Reviews).Returns(reviewRepo.Object);
        unitOfWork.SetupGet(u => u.Orders).Returns(orderRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(10);
        reviewRepo.Setup(r => r.CustomerReviewedProductAsync(102, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);  // Different product, no prior review
        orderRepo.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(10, 102, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var review = new Review { ProductId = 102, CustomerId = 10, RatingValue = 4 };
        reviewRepo.Setup(r => r.CreateAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var handler = new CreateReviewCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<CreateReviewCommandHandler>.Instance);
        var command = new CreateReviewCommand { ProductId = 102, VendorId = 20, CustomerId = 10, RatingValue = 4, Title = "Test", Comment = "Good", ImageUrls = new() };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(102, result.ProductId);
    }

    [Fact]
    public async Task CreateReview_DifferentUser_Succeeds()
    {
        // Arrange
        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var reviewRepo = new Mock<IReviewRepository>(MockBehavior.Strict);
        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Reviews).Returns(reviewRepo.Object);
        unitOfWork.SetupGet(u => u.Orders).Returns(orderRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(11);  // Different user
        reviewRepo.Setup(r => r.CustomerReviewedProductAsync(101, 11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orderRepo.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(11, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var review = new Review { ProductId = 101, CustomerId = 11, RatingValue = 5 };
        reviewRepo.Setup(r => r.CreateAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var handler = new CreateReviewCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<CreateReviewCommandHandler>.Instance);
        var command = new CreateReviewCommand { ProductId = 101, VendorId = 20, CustomerId = 11, RatingValue = 5, Title = "Test", Comment = "Good", ImageUrls = new() };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(11, result.CustomerId);
    }

    [Fact]
    public async Task CreateReview_ClientSuppliedUserIdMismatch_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var reviewRepo = new Mock<IReviewRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(10);

        var handler = new CreateReviewCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<CreateReviewCommandHandler>.Instance);
        var command = new CreateReviewCommand { ProductId = 101, VendorId = 20, CustomerId = 99, RatingValue = 5, Title = "Test", Comment = "Good", ImageUrls = new() };  // Wrong CustomerId

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateReview_UnverifiedPurchase_SetsIsVerifiedPurchaseFalse()
    {
        // Arrange
        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var reviewRepo = new Mock<IReviewRepository>(MockBehavior.Strict);
        var orderRepo = new Mock<IOrderRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        unitOfWork.SetupGet(u => u.Reviews).Returns(reviewRepo.Object);
        unitOfWork.SetupGet(u => u.Orders).Returns(orderRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(10);
        reviewRepo.Setup(r => r.CustomerReviewedProductAsync(101, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        orderRepo.Setup(o => o.CustomerHasVerifiedPurchaseOfProductAsync(10, 101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);  // No verified purchase

        var review = new Review { ProductId = 101, CustomerId = 10, RatingValue = 3, IsVerifiedPurchase = false };
        reviewRepo.Setup(r => r.CreateAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var handler = new CreateReviewCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<CreateReviewCommandHandler>.Instance);
        var command = new CreateReviewCommand { ProductId = 101, VendorId = 20, CustomerId = 10, RatingValue = 3, Title = "Test", Comment = "OK", ImageUrls = new() };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsVerifiedPurchase);
    }
}
