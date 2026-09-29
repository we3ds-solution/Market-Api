using MediatR;
using Market.Application.Abstractions.Services;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Queries
{
    public class GetCartItemCountQuery : IRequest<int>
    {
        public int UserId { get; set; }
    }

    public class GetCartItemCountQueryHandler : IRequestHandler<GetCartItemCountQuery, int>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetCartItemCountQueryHandler> _logger;

        public GetCartItemCountQueryHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<GetCartItemCountQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> Handle(GetCartItemCountQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetCartItemCountQuery for user: {UserId}", request.UserId);

            // Ownership check: reject if client tries to access different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to access count for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot access count for user {request.UserId}");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId);
            if (cart == null)
                return 0;

            return cart.TotalItems;
        }
    }
}



