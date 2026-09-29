using MediatR;
using Market.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Vendors.Commands
{
    /// <summary>
    /// Update vendor command
    /// </summary>
    public class UpdateVendorCommand : IRequest<VendorResponse>
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string StoreDescription { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public string? Banner { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? ZipCode { get; set; }
    }

    /// <summary>
    /// Update vendor command handler
    /// </summary>
    public class UpdateVendorCommandHandler : IRequestHandler<UpdateVendorCommand, VendorResponse>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UpdateVendorCommandHandler> _logger;

        public UpdateVendorCommandHandler(IMediator mediator, ILogger<UpdateVendorCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<VendorResponse> Handle(UpdateVendorCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UpdateVendorCommand for vendor: {VendorId}", request.Id);

            var result = await _mediator.Send(
                new UpdateVendorInternalCommand
                {
                    Id = request.Id,
                    StoreName = request.StoreName,
                    StoreDescription = request.StoreDescription,
                    Logo = request.Logo,
                    Banner = request.Banner,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    City = request.City,
                    Country = request.Country,
                    ZipCode = request.ZipCode
                },
                cancellationToken);

            return result;
        }
    }

    /// <summary>
    /// Internal command for updating vendor
    /// </summary>
    internal class UpdateVendorInternalCommand : IRequest<VendorResponse>
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string StoreDescription { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public string? Banner { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? ZipCode { get; set; }
    }

    /// <summary>
    /// Handler for updating vendor with ownership checks.
    /// Ensures only the vendor's owner (authenticated user) can update their vendor profile.
    /// </summary>
    internal class UpdateVendorInternalCommandHandler : IRequestHandler<UpdateVendorInternalCommand, VendorResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateVendorInternalCommandHandler> _logger;

        public UpdateVendorInternalCommandHandler(
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<UpdateVendorInternalCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<VendorResponse> Handle(UpdateVendorInternalCommand request, CancellationToken cancellationToken)
        {
            var authenticatedUserId = _currentUserService.GetCurrentUserId();

            _logger.LogInformation("Updating vendor {VendorId} by user {UserId}", request.Id, authenticatedUserId);

            // Query: get vendor
            var vendor = await _unitOfWork.Vendors.GetByIdAsync(request.Id, cancellationToken);
            if (vendor == null)
            {
                throw new KeyNotFoundException($"Vendor with ID {request.Id} not found");
            }

            // Ownership check: Can only update own vendor
            if (vendor.UserId != authenticatedUserId)
            {
                _logger.LogWarning(
                    "Unauthorized vendor update attempt: user {AuthenticatedUserId} tried to update vendor {VendorId} owned by user {OwnerId}",
                    authenticatedUserId, request.Id, vendor.UserId);
                throw new UnauthorizedAccessException($"Cannot update vendor {request.Id}");
            }

            // Update only profile fields
            vendor.StoreName = request.StoreName;
            vendor.StoreDescription = request.StoreDescription;
            vendor.Logo = request.Logo ?? vendor.Logo;
            vendor.Banner = request.Banner ?? vendor.Banner;
            vendor.PhoneNumber = request.PhoneNumber ?? vendor.PhoneNumber;
            vendor.Address = request.Address ?? vendor.Address;
            vendor.City = request.City ?? vendor.City;
            vendor.Country = request.Country ?? vendor.Country;
            vendor.ZipCode = request.ZipCode ?? vendor.ZipCode;

            // UpdatedAt is set automatically by Repository.UpdateAsync()
            await _unitOfWork.Vendors.UpdateAsync(vendor, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation("Successfully updated vendor {VendorId}", request.Id);

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



