using MediatR;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Vendors.Queries
{
    public class GetTopRatedVendorsQuery : IRequest<IEnumerable<VendorResponse>>
    {
        public int Count { get; set; } = 10;
    }

    public class GetTopRatedVendorsQueryHandler : IRequestHandler<GetTopRatedVendorsQuery, IEnumerable<VendorResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetTopRatedVendorsQueryHandler> _logger;

        public GetTopRatedVendorsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetTopRatedVendorsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<VendorResponse>> Handle(GetTopRatedVendorsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetTopRatedVendorsQuery with count: {Count}", request.Count);
            
            var vendors = await _unitOfWork.Vendors.GetTopRatedVendorsAsync(request.Count, cancellationToken);
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



