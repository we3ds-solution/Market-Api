using Market.Application.Abstractions.Services;
using Market.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Commands
{
    /// <summary>
    /// Remove from cart command
    /// </summary>
    public class RemoveFromCartCommand : IRequest<CartResponse>
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
    }

    /// <summary>
    /// Remove from cart command handler
    /// </summary>
    public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, CartResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveFromCartCommandHandler> _logger;

        public RemoveFromCartCommandHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<RemoveFromCartCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CartResponse> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling RemoveFromCartCommand for user: {UserId}, product: {ProductId}",
                request.UserId, request.ProductId);

            // Ownership check: reject if client tries to modify different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to modify cart for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot modify cart for user {request.UserId}");
            }

            // Get or create cart for user
            var cart = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId)
                      ?? throw new KeyNotFoundException($"Cart for user {request.UserId} not found");

            // Remove item from cart
            cart.RemoveItem(request.ProductId);

            // Update cart
            await _unitOfWork.Carts.UpdateAsync(cart, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return MapToResponse(cart);
        }

        private CartResponse MapToResponse(Domain.Entities.Cart cart)
        {
            return new CartResponse
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Items = cart.Items.Select(i => new CartItemResponse
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    SubTotal = i.SubTotal,
                    VendorId = i.VendorId,
                    ImageUrl = i.ImageUrl
                }).ToList(),
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt
            };
        }
    }
}



