using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Vendors.Commands
{
    /// <summary>
    /// Reject vendor command - admin only operation
    /// </summary>
    public class RejectVendorCommand : IRequest<bool>
    {
        public int Id { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Reject vendor command handler
    /// </summary>
    public class RejectVendorCommandHandler : IRequestHandler<RejectVendorCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RejectVendorCommandHandler> _logger;

        public RejectVendorCommandHandler(IMediator mediator, ILogger<RejectVendorCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(RejectVendorCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling RejectVendorCommand for vendor: {VendorId}", request.Id);

            var result = await _mediator.Send(
                new RejectVendorInternalCommand { Id = request.Id, Reason = request.Reason },
                cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Internal command for rejecting vendor
    /// </summary>
    internal class RejectVendorInternalCommand : IRequest<bool>
    {
        public int Id { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Handler for rejecting vendor.
    /// Admin-only operation that soft-deletes a vendor (typically after rejection).
    /// Preserves historical order and review data via soft-delete.
    /// </summary>
    internal class RejectVendorInternalCommandHandler : IRequestHandler<RejectVendorInternalCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RejectVendorInternalCommandHandler> _logger;

        public RejectVendorInternalCommandHandler(IUnitOfWork unitOfWork, ILogger<RejectVendorInternalCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(RejectVendorInternalCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Rejecting vendor {VendorId}. Reason: {Reason}", request.Id, request.Reason ?? "Not provided");

            var vendor = await _unitOfWork.Vendors.GetByIdAsync(request.Id, cancellationToken);
            if (vendor == null)
            {
                throw new KeyNotFoundException($"Vendor with ID {request.Id} not found");
            }

            // Soft delete: set IsDeleted=true and DeletedAt timestamp
            vendor.IsDeleted = true;
            vendor.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.Vendors.UpdateAsync(vendor, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation("Successfully rejected and soft-deleted vendor {VendorId}", request.Id);
            return true;
        }
    }
}



