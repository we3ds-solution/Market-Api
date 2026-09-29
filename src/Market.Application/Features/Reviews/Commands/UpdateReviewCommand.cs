using MediatR;
using Market.Application.Abstractions.Services;
using Market.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Reviews.Commands
{
    /// <summary>
    /// Update review command
    /// </summary>
    public class UpdateReviewCommand : IRequest<ReviewResponse>
    {
        public int Id { get; set; }
        public int RatingValue { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
    }

    /// <summary>
    /// Update review command handler
    /// </summary>
    public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, ReviewResponse>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UpdateReviewCommandHandler> _logger;

        public UpdateReviewCommandHandler(IMediator mediator, ILogger<UpdateReviewCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<ReviewResponse> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UpdateReviewCommand for review: {ReviewId}", request.Id);

            var result = await _mediator.Send(
                new UpdateReviewInternalCommand
                {
                    Id = request.Id,
                    RatingValue = request.RatingValue,
                    Title = request.Title,
                    Comment = request.Comment,
                    ImageUrls = request.ImageUrls
                },
                cancellationToken);

            return result;
        }
    }

    /// <summary>
    /// Internal command for updating review
    /// </summary>
    internal class UpdateReviewInternalCommand : IRequest<ReviewResponse>
    {
        public int Id { get; set; }
        public int RatingValue { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
    }

    /// <summary>
    /// Handler for updating a review with ownership and data integrity checks
    /// </summary>
    internal class UpdateReviewInternalCommandHandler : IRequestHandler<UpdateReviewInternalCommand, ReviewResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateReviewInternalCommandHandler> _logger;

        public UpdateReviewInternalCommandHandler(
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<UpdateReviewInternalCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ReviewResponse> Handle(UpdateReviewInternalCommand request, CancellationToken cancellationToken)
        {
            var authenticatedUserId = _currentUserService.GetCurrentUserId();

            _logger.LogInformation("Updating review {ReviewId} by user {UserId}", request.Id, authenticatedUserId);

            // Query: ReviewId first to check existence
            var review = await _unitOfWork.Reviews.GetByIdAsync(request.Id, cancellationToken);
            if (review == null)
            {
                throw new KeyNotFoundException($"Review {request.Id} not found");
            }

            // Ownership check: Can only update own reviews
            if (review.CustomerId != authenticatedUserId)
            {
                _logger.LogWarning(
                    "Unauthorized review update attempt: user {AuthenticatedUserId} tried to update review {ReviewId} owned by user {OwnerId}",
                    authenticatedUserId, request.Id, review.CustomerId);
                throw new UnauthorizedAccessException($"Cannot update review {request.Id}");
            }

            // Update only legitimately editable fields
            review.RatingValue = request.RatingValue;
            review.Title = request.Title;
            review.Comment = request.Comment;

            // Handle images
            review.Images.Clear();
            foreach (var imageUrl in request.ImageUrls)
            {
                review.Images.Add(new ReviewImage { ImageUrl = imageUrl });
            }

            // UpdatedAt is set automatically by Repository.UpdateAsync()
            await _unitOfWork.Reviews.UpdateAsync(review, cancellationToken);
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



