using MediatR;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Vendors.Queries
{
    public class GetPendingVendorsQuery : IRequest<IEnumerable<VendorResponse>>
    {
    }

    public class GetPendingVendorsQueryHandler : IRequestHandler<GetPendingVendorsQuery, IEnumerable<VendorResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetPendingVendorsQueryHandler> _logger;

        public GetPendingVendorsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetPendingVendorsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<VendorResponse>> Handle(GetPendingVendorsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetPendingVendorsQuery");
            
            var vendors = await _unitOfWork.Vendors.GetPendingVendorsAsync(cancellationToken);
            return vendors.Select(MapToResponse);
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



