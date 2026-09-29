using MediatR;
using Market.Application.Abstractions.Services;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Queries
{
    /// <summary>
    /// Get cart by user id query
    /// </summary>
    public class GetCartByUserIdQuery : IRequest<CartResponse?>
    {
        public int UserId { get; set; }
    }

    /// <summary>
    /// Get cart by user id query handler
    /// </summary>
    public class GetCartByUserIdQueryHandler : IRequestHandler<GetCartByUserIdQuery, CartResponse?>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetCartByUserIdQueryHandler> _logger;

        public GetCartByUserIdQueryHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<GetCartByUserIdQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CartResponse?> Handle(GetCartByUserIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetCartByUserIdQuery for user: {UserId}", request.UserId);

            // Ownership check: reject if client tries to access different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to access cart for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot access cart for user {request.UserId}");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId);

            if (cart == null)
                return null;

            return new CartResponse
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Items = cart.Items.Select(i => new CartItemResponse
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    VendorId = i.VendorId,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    ImageUrl = i.ImageUrl,
                    SubTotal = i.SubTotal
                }).ToList(),
                TotalPrice = cart.SubTotal,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt
            };
        }
    }
}



