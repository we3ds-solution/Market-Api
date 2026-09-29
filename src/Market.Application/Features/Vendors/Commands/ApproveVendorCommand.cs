using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Vendors.Commands
{
    /// <summary>
    /// Approve vendor command - admin only operation
    /// </summary>
    public class ApproveVendorCommand : IRequest<VendorResponse>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Approve vendor command handler
    /// </summary>
    public class ApproveVendorCommandHandler : IRequestHandler<ApproveVendorCommand, VendorResponse>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ApproveVendorCommandHandler> _logger;

        public ApproveVendorCommandHandler(IMediator mediator, ILogger<ApproveVendorCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<VendorResponse> Handle(ApproveVendorCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling ApproveVendorCommand for vendor: {VendorId}", request.Id);

            var result = await _mediator.Send(new ApproveVendorInternalCommand { Id = request.Id }, cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Internal command for approving vendor
    /// </summary>
    internal class ApproveVendorInternalCommand : IRequest<VendorResponse>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Handler for approving vendor.
    /// Admin-only operation that transitions vendor from Pending (IsApproved=false) to Approved (IsApproved=true).
    /// Only vendors that are approved and active can create products visible to customers.
    /// </summary>
    internal class ApproveVendorInternalCommandHandler : IRequestHandler<ApproveVendorInternalCommand, VendorResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ApproveVendorInternalCommandHandler> _logger;

        public ApproveVendorInternalCommandHandler(IUnitOfWork unitOfWork, ILogger<ApproveVendorInternalCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<VendorResponse> Handle(ApproveVendorInternalCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Approving vendor {VendorId}", request.Id);

            var vendor = await _unitOfWork.Vendors.GetByIdAsync(request.Id, cancellationToken);
            if (vendor == null)
            {
                throw new KeyNotFoundException($"Vendor with ID {request.Id} not found");
            }

            // Transition: Pending → Approved
            vendor.IsApproved = true;
            await _unitOfWork.Vendors.UpdateAsync(vendor, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation("Successfully approved vendor {VendorId}", request.Id);

            return MapToResponse(vendor);
        }

        private VendorResponse MapToResponse(Market.Domain.Entities.Vendor vendor)
        {
            return new VendorResponse
            {
                Id = vendor.Id,
                UserId = vendor.UserId,
                StoreName = vendor.StoreName,
                StoreDescription = vendor.StoreDescription,
                PhoneNumber = vendor.PhoneNumber,
                CommissionRate = vendor.CommissionRate,
                IsApproved = vendor.IsApproved,
                IsActive = vendor.IsActive,
                AverageRating = vendor.AverageRating,
                TotalReviews = vendor.TotalReviews,
                CreatedAt = vendor.CreatedAt,
                UpdatedAt = vendor.UpdatedAt
            };
        }
    }
}



