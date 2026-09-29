using Market.Application.Features.Carts.Commands;
using Microsoft.Extensions.Logging.Abstractions;

namespace Market.Application.Tests.Features.Carts;

public class UpdateCartItemQuantityHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingItem_UpdatesAndPersistsCart()
    {
        var cart = new Cart { Id = 8, UserId = 3, Items = [new CartItem { ProductId = 4, Price = 9m, Quantity = 1 }] };
        var carts = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        carts.Setup(repository => repository.GetByUserIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(cart);
        carts.Setup(repository => repository.UpdateAsync(cart, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.SetupGet(value => value.Carts).Returns(carts.Object);
        unitOfWork.Setup(value => value.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(3); var handler = new UpdateCartItemQuantityCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<UpdateCartItemQuantityCommandHandler>.Instance);

        var result = await handler.Handle(new UpdateCartItemQuantityCommand { UserId = 3, ProductId = 4, Quantity = 5 }, CancellationToken.None);

        Assert.Equal(5, Assert.Single(result.Items).Quantity);
        Assert.Equal(45m, result.Items.Single().SubTotal);
        unitOfWork.Verify(value => value.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonPositiveQuantity_RejectsBeforePersistence()
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUserServiceMock = new Mock<Market.Application.Abstractions.Services.ICurrentUserService>(); currentUserServiceMock.Setup(s => s.GetCurrentUserId()).Returns(3); var handler = new UpdateCartItemQuantityCommandHandler(currentUserServiceMock.Object, unitOfWork.Object, NullLogger<UpdateCartItemQuantityCommandHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new UpdateCartItemQuantityCommand { UserId = 3, ProductId = 4, Quantity = 0 }, CancellationToken.None));

        unitOfWork.Verify(value => value.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

