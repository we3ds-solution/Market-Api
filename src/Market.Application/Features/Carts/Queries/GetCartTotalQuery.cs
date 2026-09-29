using MediatR;
using Market.Application.Abstractions.Services;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Queries
{
    public class GetCartTotalQuery : IRequest<decimal>
    {
        public int UserId { get; set; }
    }

    public class GetCartTotalQueryHandler : IRequestHandler<GetCartTotalQuery, decimal>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetCartTotalQueryHandler> _logger;

        public GetCartTotalQueryHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<GetCartTotalQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<decimal> Handle(GetCartTotalQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetCartTotalQuery for user: {UserId}", request.UserId);

            // Ownership check: reject if client tries to access different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to access total for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot access total for user {request.UserId}");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId);
            if (cart == null)
                return 0M;

            return cart.SubTotal;
        }
    }
}



