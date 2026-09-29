using MediatR;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Vendors.Queries
{
    public class GetApprovedVendorsQuery : IRequest<IEnumerable<VendorResponse>>
    {
    }

    public class GetApprovedVendorsQueryHandler : IRequestHandler<GetApprovedVendorsQuery, IEnumerable<VendorResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetApprovedVendorsQueryHandler> _logger;

        public GetApprovedVendorsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetApprovedVendorsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<VendorResponse>> Handle(GetApprovedVendorsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetApprovedVendorsQuery");
            
            var vendors = await _unitOfWork.Vendors.GetApprovedVendorsAsync(cancellationToken);
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



