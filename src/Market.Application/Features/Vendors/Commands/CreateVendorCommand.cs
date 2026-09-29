using MediatR;
using Market.Application.Abstractions.Services;
using Market.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Vendors.Commands
{
    /// <summary>
    /// Create vendor command
    /// </summary>
    public class CreateVendorCommand : IRequest<VendorResponse>
    {
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
    /// Create vendor command handler
    /// </summary>
    public class CreateVendorCommandHandler : IRequestHandler<CreateVendorCommand, VendorResponse>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CreateVendorCommandHandler> _logger;

        public CreateVendorCommandHandler(IMediator mediator, ILogger<CreateVendorCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<VendorResponse> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling CreateVendorCommand for vendor: {StoreName}", request.StoreName);

            var result = await _mediator.Send(
                new CreateVendorInternalCommand
                {
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
    /// Internal command for creating vendor with current-user ownership
    /// </summary>
    internal class CreateVendorInternalCommand : IRequest<VendorResponse>
    {
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
    /// Handler for creating vendor with ownership checks.
    /// Ensures only authenticated users can create vendors for themselves.
    /// New vendors start in Pending state (IsApproved=false).
    /// </summary>
    internal class CreateVendorInternalCommandHandler : IRequestHandler<CreateVendorInternalCommand, VendorResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateVendorInternalCommandHandler> _logger;

        public CreateVendorInternalCommandHandler(
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<CreateVendorInternalCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<VendorResponse> Handle(CreateVendorInternalCommand request, CancellationToken cancellationToken)
        {
            var authenticatedUserId = _currentUserService.GetCurrentUserId();

            _logger.LogInformation("Creating vendor for user {UserId}: {StoreName}", authenticatedUserId, request.StoreName);

            // Check if user already has a vendor
            var existingVendor = await _unitOfWork.Vendors.GetByUserIdAsync(authenticatedUserId, cancellationToken);
            if (existingVendor != null)
            {
                _logger.LogWarning("User {UserId} already has a vendor {VendorId}. Duplicate vendor creation rejected.",
                    authenticatedUserId, existingVendor.Id);
                throw new InvalidOperationException($"User already has a vendor. Cannot create multiple vendors per user.");
            }

            var vendor = new Vendor
            {
                UserId = authenticatedUserId,
                StoreName = request.StoreName,
                StoreDescription = request.StoreDescription,
                Logo = request.Logo ?? string.Empty,
                Banner = request.Banner ?? string.Empty,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                City = request.City,
                Country = request.Country,
                ZipCode = request.ZipCode,
                IsApproved = false,  // Start as Pending (not approved)
                IsActive = true      // But initially active for profile management
            };

            await _unitOfWork.Vendors.CreateAsync(vendor, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation("Successfully created vendor {VendorId} for user {UserId}", vendor.Id, authenticatedUserId);

            return MapToResponse(vendor);
        }

        private VendorResponse MapToResponse(Vendor vendor)
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



