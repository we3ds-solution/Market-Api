using MediatR;
using Market.Domain.Entities;
using Market.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Reviews.Commands
{
    /// <summary>
    /// Create review command
    /// </summary>
    public class CreateReviewCommand : IRequest<ReviewResponse>
    {
        public int ProductId { get; set; }
        public int VendorId { get; set; }
        public int CustomerId { get; set; }
        public int RatingValue { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
    }

    /// <summary>
    /// Create review command handler
    /// </summary>
    public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateReviewCommandHandler> _logger;

        public CreateReviewCommandHandler(
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<CreateReviewCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ReviewResponse> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
        {
            // Ownership check: authenticated user creates review for themselves only
            var authenticatedUserId = _currentUserService.GetCurrentUserId();
            if (request.CustomerId != authenticatedUserId)
            {
                _logger.LogWarning("Unauthorized review attempt: user {AuthenticatedUserId} tried to create review for user {RequestedUserId}",
                    authenticatedUserId, request.CustomerId);
                throw new UnauthorizedAccessException($"Cannot create review for user {request.CustomerId}");
            }

            _logger.LogInformation("Handling CreateReviewCommand for product: {ProductId} by customer: {CustomerId}",
                request.ProductId, request.CustomerId);

            // Check if customer already reviewed this product
            var existingReview = await _unitOfWork.Reviews.CustomerReviewedProductAsync(
                request.ProductId, request.CustomerId, cancellationToken);

            if (existingReview)
            {
                throw new InvalidOperationException(
                    $"Customer {request.CustomerId} has already reviewed product {request.ProductId}");
            }

            // Check if customer has a verified purchase of this product
            var hasVerifiedPurchase = await _unitOfWork.Orders.CustomerHasVerifiedPurchaseOfProductAsync(
                request.CustomerId, request.ProductId, cancellationToken);

            var review = new Review
            {
                ProductId = request.ProductId,
                VendorId = request.VendorId,
                CustomerId = request.CustomerId,
                RatingValue = request.RatingValue,
                Title = request.Title,
                Comment = request.Comment,
                IsVerifiedPurchase = hasVerifiedPurchase
            };

            // Add images as ReviewImage entities
            foreach (var imageUrl in request.ImageUrls)
            {
                review.Images.Add(new ReviewImage { ImageUrl = imageUrl });
            }

            await _unitOfWork.Reviews.CreateAsync(review, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return new ReviewResponse
            {
                Id = review.Id,
                ProductId = review.ProductId,
                VendorId = review.VendorId,
                CustomerId = review.CustomerId,
                RatingValue = review.RatingValue,
                Title = review.Title,
                Comment = review.Comment,
                ImageUrls = review.Images.Select(i => i.ImageUrl).ToList(),
                HelpfulCount = review.HelpfulCount,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };
        }
    }
}



