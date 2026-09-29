using MediatR;
using Market.Application.Abstractions.Services;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Queries
{
    public class GetCartItemsQuery : IRequest<IEnumerable<CartItemResponse>>
    {
        public int UserId { get; set; }
    }

    public class GetCartItemsQueryHandler : IRequestHandler<GetCartItemsQuery, IEnumerable<CartItemResponse>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetCartItemsQueryHandler> _logger;

        public GetCartItemsQueryHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<GetCartItemsQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<CartItemResponse>> Handle(GetCartItemsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetCartItemsQuery for user: {UserId}", request.UserId);

            // Ownership check: reject if client tries to access different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to access items for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot access items for user {request.UserId}");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId);
            if (cart == null)
                return Enumerable.Empty<CartItemResponse>();

            return cart.Items.Select(i => new CartItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                VendorId = i.VendorId,
                Price = i.Price,
                Quantity = i.Quantity,
                ImageUrl = i.ImageUrl,
                SubTotal = i.SubTotal
            }).ToList();
        }
    }
}



