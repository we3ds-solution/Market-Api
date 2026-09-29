using MediatR;
using Market.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Reviews.Commands
{
    /// <summary>
    /// Delete review command
    /// </summary>
    public class DeleteReviewCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Delete review command handler
    /// </summary>
    public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DeleteReviewCommandHandler> _logger;

        public DeleteReviewCommandHandler(IMediator mediator, ILogger<DeleteReviewCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling DeleteReviewCommand for review: {ReviewId}", request.Id);

            var result = await _mediator.Send(new DeleteReviewInternalCommand { Id = request.Id }, cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Internal command for deleting review
    /// </summary>
    internal class DeleteReviewInternalCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Handler for deleting a review with ownership checks
    /// </summary>
    internal class DeleteReviewInternalCommandHandler : IRequestHandler<DeleteReviewInternalCommand, bool>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteReviewInternalCommandHandler> _logger;

        public DeleteReviewInternalCommandHandler(
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<DeleteReviewInternalCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteReviewInternalCommand request, CancellationToken cancellationToken)
        {
            var authenticatedUserId = _currentUserService.GetCurrentUserId();

            _logger.LogInformation("Deleting review {ReviewId} by user {UserId}", request.Id, authenticatedUserId);

            // Query: ReviewId first to check existence
            var review = await _unitOfWork.Reviews.GetByIdAsync(request.Id, cancellationToken);
            if (review == null)
            {
                throw new KeyNotFoundException($"Review {request.Id} not found");
            }

            // Ownership check: Can only delete own reviews
            if (review.CustomerId != authenticatedUserId)
            {
                _logger.LogWarning(
                    "Unauthorized review deletion attempt: user {AuthenticatedUserId} tried to delete review {ReviewId} owned by user {OwnerId}",
                    authenticatedUserId, request.Id, review.CustomerId);
                throw new UnauthorizedAccessException($"Cannot delete review {request.Id}");
            }

            // Soft delete
            review.IsDeleted = true;
            await _unitOfWork.Reviews.UpdateAsync(review, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted review {ReviewId}", request.Id);
            return true;
        }
    }
}



