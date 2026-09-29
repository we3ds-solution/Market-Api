using MediatR;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Market.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Commands
{
    /// <summary>
    /// Add to cart command
    /// </summary>
    public class AddToCartCommand : IRequest<CartResponse>
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Add to cart command handler
    /// </summary>
    public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, CartResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddToCartCommandHandler> _logger;

        public AddToCartCommandHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<AddToCartCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CartResponse> Handle(AddToCartCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling AddToCartCommand for user: {UserId}, product: {ProductId}",
                request.UserId, request.ProductId);

            // Ownership check: reject if client tries to add to different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to modify cart for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot modify cart for user {request.UserId}");
            }

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0", nameof(request.Quantity));

            // Fetch product from database
            var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null)
                throw new KeyNotFoundException($"Product {request.ProductId} not found");

            if (product.Status != ProductStatus.Active)
                throw new InvalidOperationException($"Product {request.ProductId} is not active");

            // Calculate unit price using server-side logic (reuse Issue #1 pricing logic)
            var unitPrice = product.DiscountPrice ?? product.Price;

            // Create CartItem with server-controlled values
            var cartItem = new CartItem
            {
                ProductId = product.Id,
                VendorId = product.VendorId,
                ProductName = product.Name,
                Quantity = request.Quantity,
                Price = unitPrice,
                ImageUrl = product.ImageUrl
            };

            var result = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId, cancellationToken)
                      ?? new Domain.Entities.Cart { UserId = request.UserId };

            result.AddItem(cartItem);

            if (result.Id == 0)
            {
                await _unitOfWork.Carts.CreateAsync(result, cancellationToken);
            }
            else
            {
                await _unitOfWork.Carts.UpdateAsync(result, cancellationToken);
            }

            await _unitOfWork.SaveAsync(cancellationToken);

            return MapToResponse(result);
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

