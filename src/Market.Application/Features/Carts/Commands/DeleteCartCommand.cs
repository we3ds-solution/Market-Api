using Market.Application.Abstractions.Services;
using Market.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Carts.Commands
{
    public class DeleteCartCommand : IRequest<bool>
    {
        public int UserId { get; set; }
    }

    public class DeleteCartCommandHandler : IRequestHandler<DeleteCartCommand, bool>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteCartCommandHandler> _logger;

        public DeleteCartCommandHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<DeleteCartCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteCartCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling DeleteCartCommand for user: {UserId}", request.UserId);

            // Ownership check: reject if client tries to delete different user's cart
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.UserId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized cart access attempt: user {AuthenticatedUserId} tried to delete cart for user {RequestedUserId}",
                    authenticatedUserId, request.UserId);
                throw new UnauthorizedAccessException($"Cannot delete cart for user {request.UserId}");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(request.UserId);
            if (cart == null)
                return false;

            await _unitOfWork.Carts.DeleteAsync(cart.Id, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return true;
        }
    }
}



