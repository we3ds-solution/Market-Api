using Market.Application.Abstractions.Services;
using Market.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Commands
{
    /// <summary>
    /// Clear cart command
    /// </summary>
    public class ClearCartCommand : IRequest<bool>
    {
        public int UserId { get; set; }
    }

    /// <summary>
    /// Clear cart command handler
    /// </summary>
    public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, bool>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ClearCartCommandHandler> _logger;

        public ClearCartCommandHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<ClearCartCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(ClearCartCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling ClearCartCommand for user: {UserId}", request.UserId);

            // Ownership check: reject if client tries to modify different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to modify cart for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot modify cart for user {request.UserId}");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId);
            if (cart == null)
                return false;

            // Clear all items from cart
            cart.Clear();

            // Update cart
            await _unitOfWork.Carts.UpdateAsync(cart, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return true;
        }
    }
}



