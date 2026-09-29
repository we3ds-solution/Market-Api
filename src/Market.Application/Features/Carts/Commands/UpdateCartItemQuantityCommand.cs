using Market.Application.Abstractions.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Commands
{
    /// <summary>
    /// Update cart item quantity command
    /// </summary>
    public class UpdateCartItemQuantityCommand : IRequest<CartResponse>
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Update cart item quantity command handler
    /// </summary>
    public class UpdateCartItemQuantityCommandHandler : IRequestHandler<UpdateCartItemQuantityCommand, CartResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateCartItemQuantityCommandHandler> _logger;

        public UpdateCartItemQuantityCommandHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<UpdateCartItemQuantityCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CartResponse> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating cart item quantity for user {UserId}, product {ProductId} to {Quantity}",
                request.UserId, request.ProductId, request.Quantity);

            // Ownership check: reject if client tries to modify different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to modify cart for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot modify cart for user {request.UserId}");
            }

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0", nameof(request.Quantity));

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId);
            if (cart == null)
                throw new KeyNotFoundException($"Cart not found for user {request.UserId}");

            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (item == null)
                throw new KeyNotFoundException($"Product {request.ProductId} not found in cart");

            item.Quantity = request.Quantity;
            await _unitOfWork.Carts.UpdateAsync(cart, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return MapToResponse(cart);
        }

        private CartResponse MapToResponse(Market.Domain.Entities.Cart cart)
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



