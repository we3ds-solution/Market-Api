using MediatR;
using Market.Application.Abstractions.Services;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Queries
{
    public class GetOrCreateCartQuery : IRequest<CartResponse?>
    {
        public int UserId { get; set; }
    }

    public class GetOrCreateCartQueryHandler : IRequestHandler<GetOrCreateCartQuery, CartResponse?>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetOrCreateCartQueryHandler> _logger;

        public GetOrCreateCartQueryHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<GetOrCreateCartQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CartResponse?> Handle(GetOrCreateCartQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetOrCreateCartQuery for user: {UserId}", request.UserId);

            // Ownership check: reject if client tries to access different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to get/create cart for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot get/create cart for user {request.UserId}");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId);

            // If cart doesn't exist, create a new one
            if (cart == null)
            {
                cart = new Market.Domain.Entities.Cart { UserId = request.UserId };
                await _unitOfWork.Carts.CreateAsync(cart, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
            }

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



