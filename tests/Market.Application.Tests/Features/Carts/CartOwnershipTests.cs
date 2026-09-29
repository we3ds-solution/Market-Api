using MediatR;
using Market.Application.Features.Carts.Commands;
using Market.Application.Features.Carts.Queries;
using Market.Application.Abstractions.Services;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Market.Application.Tests.Features.Carts;

/// <summary>
/// Comprehensive tests for cart ownership enforcement.
/// Validates that users can only access and modify their own carts.
/// </summary>
public class CartOwnershipTests
{
    /// <summary>
    /// Test 1: User_Can_Access_Own_Cart
    /// Validates that a user can retrieve their own cart
    /// </summary>
    [Fact]
    public async Task User_Can_Access_Own_Cart()
    {
        // Arrange
        var userId = 1;
        var cart = new Cart { Id = 100, UserId = userId };

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var cartRepository = new Mock<ICartRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        currentUserService
            .Setup(s => s.GetCurrentUserId())
            .Returns(userId);

        cartRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        unitOfWork.SetupGet(u => u.Carts).Returns(cartRepository.Object);

        var handler = new GetCartByUserIdQueryHandler(currentUserService.Object, unitOfWork.Object, NullLogger<GetCartByUserIdQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetCartByUserIdQuery { UserId = userId }, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
    }

    /// <summary>
    /// Test 2: User_Cannot_Retrieve_Other_User_Cart
    /// Validates that a user cannot retrieve another user's cart
    /// </summary>
    [Fact]
    public async Task User_Cannot_Retrieve_Other_User_Cart()
    {
        // Arrange
        var authenticatedUserId = 1;
        var otherUserId = 2;

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        currentUserService
            .Setup(s => s.GetCurrentUserId())
            .Returns(authenticatedUserId);

        var handler = new GetCartByUserIdQueryHandler(currentUserService.Object, unitOfWork.Object, NullLogger<GetCartByUserIdQueryHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new GetCartByUserIdQuery { UserId = otherUserId }, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test 3: User_Cannot_Add_To_Other_User_Cart
    /// Validates that a user cannot add items to another user's cart
    /// </summary>
    [Fact]
    public async Task User_Cannot_Add_To_Other_User_Cart()
    {
        // Arrange
        var authenticatedUserId = 1;
        var otherUserId = 2;

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);

        currentUserService
            .Setup(s => s.GetCurrentUserId())
            .Returns(authenticatedUserId);

        var handler = new AddToCartCommandHandler(currentUserService.Object, new Mock<IUnitOfWork>().Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new AddToCartCommand { UserId = otherUserId, ProductId = 1, Quantity = 1 }, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test 4: User_Cannot_Update_Other_User_Cart
    /// Validates that a user cannot update quantity in another user's cart
    /// </summary>
    [Fact]
    public async Task User_Cannot_Update_Other_User_Cart()
    {
        // Arrange
        var authenticatedUserId = 1;
        var otherUserId = 2;

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);

        currentUserService
            .Setup(s => s.GetCurrentUserId())
            .Returns(authenticatedUserId);

        var handler = new UpdateCartItemQuantityCommandHandler(currentUserService.Object, new Mock<IUnitOfWork>().Object, NullLogger<UpdateCartItemQuantityCommandHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new UpdateCartItemQuantityCommand { UserId = otherUserId, ProductId = 1, Quantity = 5 }, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test 5: User_Cannot_Remove_From_Other_User_Cart
    /// Validates that a user cannot remove items from another user's cart
    /// </summary>
    [Fact]
    public async Task User_Cannot_Remove_From_Other_User_Cart()
    {
        // Arrange
        var authenticatedUserId = 1;
        var otherUserId = 2;

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);

        currentUserService
            .Setup(s => s.GetCurrentUserId())
            .Returns(authenticatedUserId);

        var handler = new RemoveFromCartCommandHandler(currentUserService.Object, new Mock<IUnitOfWork>().Object, NullLogger<RemoveFromCartCommandHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RemoveFromCartCommand { UserId = otherUserId, ProductId = 1 }, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test 6: User_Cannot_Clear_Other_User_Cart
    /// Validates that a user cannot clear another user's cart
    /// </summary>
    [Fact]
    public async Task User_Cannot_Clear_Other_User_Cart()
    {
        // Arrange
        var authenticatedUserId = 1;
        var otherUserId = 2;

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);

        currentUserService
            .Setup(s => s.GetCurrentUserId())
            .Returns(authenticatedUserId);

        var handler = new ClearCartCommandHandler(currentUserService.Object, new Mock<IUnitOfWork>().Object, NullLogger<ClearCartCommandHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new ClearCartCommand { UserId = otherUserId }, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test 7: User_Cannot_Delete_Other_User_Cart
    /// Validates that a user cannot delete another user's cart
    /// </summary>
    [Fact]
    public async Task User_Cannot_Delete_Other_User_Cart()
    {
        // Arrange
        var authenticatedUserId = 1;
        var otherUserId = 2;

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);

        currentUserService
            .Setup(s => s.GetCurrentUserId())
            .Returns(authenticatedUserId);

        var handler = new DeleteCartCommandHandler(currentUserService.Object, new Mock<IUnitOfWork>().Object, NullLogger<DeleteCartCommandHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new DeleteCartCommand { UserId = otherUserId }, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test 8: Client_Supplied_UserId_Cannot_Override_Authentication
    /// Validates that clients cannot override their authenticated user ID by supplying a different UserId in request
    /// This is the core security validation - client cannot escalate privileges by manipulating UserId parameter
    /// </summary>
    [Fact]
    public async Task Client_Supplied_UserId_Cannot_Override_Authentication()
    {
        // Arrange
        var authenticatedUserId = 1;
        var maliciousUserId = 999; // Client attempts to add to user 999's cart

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);

        currentUserService
            .Setup(s => s.GetCurrentUserId())
            .Returns(authenticatedUserId);

        var handler = new AddToCartCommandHandler(currentUserService.Object, new Mock<IUnitOfWork>().Object, NullLogger<AddToCartCommandHandler>.Instance);

        var command = new AddToCartCommand
        {
            UserId = maliciousUserId,  // Client tries to manipulate
            ProductId = 1,
            Quantity = 1
        };

        // Act & Assert - Should always throw exception, never allow client to override
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(command, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test 9: Unauthenticated_Request_Throws_Exception
    /// Validates that unauthenticated requests throw an exception when trying to access ICurrentUserService
    /// </summary>
    [Fact]
    public async Task Unauthenticated_Request_Throws_Exception()
    {
        // Arrange
        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);

        currentUserService
            .Setup(s => s.GetCurrentUserId())
            .Throws<UnauthorizedAccessException>();

        var handler = new AddToCartCommandHandler(currentUserService.Object, new Mock<IUnitOfWork>().Object, NullLogger<AddToCartCommandHandler>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new AddToCartCommand { UserId = 1, ProductId = 1, Quantity = 1 }, CancellationToken.None)
        );
    }

    /// <summary>
    /// Test 10: All_Queries_Enforce_Ownership
    /// Validates that all cart queries enforce ownership checks
    /// </summary>
    [Fact]
    public async Task All_Queries_Enforce_Ownership()
    {
        // Arrange
        var authenticatedUserId = 1;
        var otherUserId = 2;

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(authenticatedUserId);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var queries = new List<object>
        {
            new GetCartByUserIdQuery { UserId = otherUserId },
            new GetCartItemsQuery { UserId = otherUserId },
            new GetCartTotalQuery { UserId = otherUserId },
            new GetCartItemCountQuery { UserId = otherUserId },
            new GetOrCreateCartQuery { UserId = otherUserId }
        };

        // Test each query
        var getCartHandler = new GetCartByUserIdQueryHandler(currentUserService.Object, unitOfWork.Object, NullLogger<GetCartByUserIdQueryHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => getCartHandler.Handle(queries[0] as GetCartByUserIdQuery, CancellationToken.None));

        var getItemsHandler = new GetCartItemsQueryHandler(currentUserService.Object, unitOfWork.Object, NullLogger<GetCartItemsQueryHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => getItemsHandler.Handle(queries[1] as GetCartItemsQuery, CancellationToken.None));

        var getTotalHandler = new GetCartTotalQueryHandler(currentUserService.Object, unitOfWork.Object, NullLogger<GetCartTotalQueryHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => getTotalHandler.Handle(queries[2] as GetCartTotalQuery, CancellationToken.None));

        var getCountHandler = new GetCartItemCountQueryHandler(currentUserService.Object, unitOfWork.Object, NullLogger<GetCartItemCountQueryHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => getCountHandler.Handle(queries[3] as GetCartItemCountQuery, CancellationToken.None));

        var getOrCreateHandler = new GetOrCreateCartQueryHandler(currentUserService.Object, unitOfWork.Object, NullLogger<GetOrCreateCartQueryHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => getOrCreateHandler.Handle(queries[4] as GetOrCreateCartQuery, CancellationToken.None));
    }

    /// <summary>
    /// Test 11: All_Commands_Enforce_Ownership
    /// Validates that all cart commands enforce ownership checks
    /// </summary>
    [Fact]
    public async Task All_Commands_Enforce_Ownership()
    {
        // Arrange
        var authenticatedUserId = 1;
        var otherUserId = 2;

        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(authenticatedUserId);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        // Test AddToCartCommand
        var addHandler = new AddToCartCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<AddToCartCommandHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => addHandler.Handle(new AddToCartCommand { UserId = otherUserId, ProductId = 1, Quantity = 1 }, CancellationToken.None));

        // Test RemoveFromCartCommand
        var removeHandler = new RemoveFromCartCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<RemoveFromCartCommandHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => removeHandler.Handle(new RemoveFromCartCommand { UserId = otherUserId, ProductId = 1 }, CancellationToken.None));

        // Test UpdateCartItemQuantityCommand
        var updateHandler = new UpdateCartItemQuantityCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<UpdateCartItemQuantityCommandHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => updateHandler.Handle(new UpdateCartItemQuantityCommand { UserId = otherUserId, ProductId = 1, Quantity = 5 }, CancellationToken.None));

        // Test ClearCartCommand
        var clearHandler = new ClearCartCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<ClearCartCommandHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => clearHandler.Handle(new ClearCartCommand { UserId = otherUserId }, CancellationToken.None));

        // Test DeleteCartCommand
        var deleteHandler = new DeleteCartCommandHandler(currentUserService.Object, unitOfWork.Object, NullLogger<DeleteCartCommandHandler>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => deleteHandler.Handle(new DeleteCartCommand { UserId = otherUserId }, CancellationToken.None));
    }
}
