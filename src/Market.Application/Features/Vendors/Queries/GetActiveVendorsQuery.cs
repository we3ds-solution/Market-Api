using MediatR;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Vendors.Queries
{
    public class GetActiveVendorsQuery : IRequest<IEnumerable<VendorResponse>>
    {
    }

    public class GetActiveVendorsQueryHandler : IRequestHandler<GetActiveVendorsQuery, IEnumerable<VendorResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetActiveVendorsQueryHandler> _logger;

        public GetActiveVendorsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetActiveVendorsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<VendorResponse>> Handle(GetActiveVendorsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetActiveVendorsQuery");
            
            var vendors = await _unitOfWork.Vendors.GetActiveVendorsAsync(cancellationToken);
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



