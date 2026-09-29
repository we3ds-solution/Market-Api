using MediatR;
using Market.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Vendors.Commands
{
    /// <summary>
    /// Delete vendor command
    /// </summary>
    public class DeleteVendorCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Delete vendor command handler
    /// </summary>
    public class DeleteVendorCommandHandler : IRequestHandler<DeleteVendorCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DeleteVendorCommandHandler> _logger;

        public DeleteVendorCommandHandler(IMediator mediator, ILogger<DeleteVendorCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteVendorCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling DeleteVendorCommand for vendor: {VendorId}", request.Id);

            var result = await _mediator.Send(new DeleteVendorInternalCommand { Id = request.Id }, cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Internal command for deleting vendor
    /// </summary>
    internal class DeleteVendorInternalCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Handler for deleting vendor with ownership checks and business rule validation.
    /// Ensures only the vendor's owner can delete their vendor.
    /// Vendors are soft-deleted to preserve historical order and review data.
    /// </summary>
    internal class DeleteVendorInternalCommandHandler : IRequestHandler<DeleteVendorInternalCommand, bool>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteVendorInternalCommandHandler> _logger;

        public DeleteVendorInternalCommandHandler(
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<DeleteVendorInternalCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteVendorInternalCommand request, CancellationToken cancellationToken)
        {
            var authenticatedUserId = _currentUserService.GetCurrentUserId();

            _logger.LogInformation("Deleting vendor {VendorId} by user {UserId}", request.Id, authenticatedUserId);

            // Query: get vendor
            var vendor = await _unitOfWork.Vendors.GetByIdAsync(request.Id, cancellationToken);
            if (vendor == null)
            {
                throw new KeyNotFoundException($"Vendor with ID {request.Id} not found");
            }

            // Ownership check: Can only delete own vendor
            if (vendor.UserId != authenticatedUserId)
            {
                _logger.LogWarning(
                    "Unauthorized vendor deletion attempt: user {AuthenticatedUserId} tried to delete vendor {VendorId} owned by user {OwnerId}",
                    authenticatedUserId, request.Id, vendor.UserId);
                throw new UnauthorizedAccessException($"Cannot delete vendor {request.Id}");
            }

            // Soft delete: set IsDeleted=true and DeletedAt timestamp
            vendor.IsDeleted = true;
            vendor.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.Vendors.UpdateAsync(vendor, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation("Successfully soft-deleted vendor {VendorId}", request.Id);
            return true;
        }
    }
}



