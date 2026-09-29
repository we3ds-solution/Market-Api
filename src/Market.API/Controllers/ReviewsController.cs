using MediatR;
using Market.Application.Features.Reviews.Commands;
using Market.Application.Features.Reviews.Queries;

namespace Market.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(IMediator mediator, ILogger<ReviewsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get all reviews
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var query = new GetAllReviewsQuery();
            var reviews = await _mediator.Send(query);
            return Ok(reviews);
        }

        /// <summary>
        /// Get review by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var query = new GetReviewByIdQuery { Id = id };
            var review = await _mediator.Send(query);
            if (review == null)
                return NotFound();

            return Ok(review);
        }

        /// <summary>
        /// Get reviews by product
        /// </summary>
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var query = new GetReviewsByProductQuery { ProductId = productId };
            var reviews = await _mediator.Send(query);
            return Ok(reviews);
        }

        /// <summary>
        /// Get reviews by vendor
        /// </summary>
        [HttpGet("vendor/{vendorId}")]
        public async Task<IActionResult> GetByVendor(int vendorId)
        {
            var query = new GetReviewsByVendorQuery { VendorId = vendorId };
            var reviews = await _mediator.Send(query);
            return Ok(reviews);
        }

        /// <summary>
        /// Get reviews by customer
        /// </summary>
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var query = new GetReviewsByCustomerQuery { CustomerId = customerId };
            var reviews = await _mediator.Send(query);
            return Ok(reviews);
        }

        /// <summary>
        /// Get verified reviews for product
        /// </summary>
        [HttpGet("product/{productId}/verified")]
        public async Task<IActionResult> GetVerified(int productId)
        {
            var query = new GetVerifiedReviewsQuery { ProductId = productId };
            var reviews = await _mediator.Send(query);
            return Ok(reviews);
        }

        /// <summary>
        /// Get reviews by rating
        /// </summary>
        [HttpGet("product/{productId}/rating/{rating}")]
        public async Task<IActionResult> GetByRating(int productId, int rating)
        {
            try
            {
                var query = new GetReviewsByRatingQuery { ProductId = productId, Rating = rating };
                var reviews = await _mediator.Send(query);
                return Ok(reviews);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid rating: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get top helpful reviews
        /// </summary>
        [HttpGet("product/{productId}/helpful")]
        public async Task<IActionResult> GetTopHelpful(int productId, [FromQuery] int count = 10)
        {
            var query = new GetTopHelpfulReviewsQuery { ProductId = productId, Count = count };
            var reviews = await _mediator.Send(query);
            return Ok(reviews);
        }

        /// <summary>
        /// Get average rating for product
        /// </summary>
        [HttpGet("product/{productId}/average-rating")]
        public async Task<IActionResult> GetAverageRating(int productId)
        {
            var query = new GetAverageRatingQuery { ProductId = productId };
            var average = await _mediator.Send(query);
            return Ok(new { averageRating = average });
        }

        /// <summary>
        /// Get rating distribution for product
        /// </summary>
        [HttpGet("product/{productId}/rating-distribution")]
        public async Task<IActionResult> GetRatingDistribution(int productId)
        {
            var query = new GetRatingDistributionQuery { ProductId = productId };
            var distribution = await _mediator.Send(query);
            return Ok(distribution);
        }

        /// <summary>
        /// Create a new review
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("write")]
        public async Task<IActionResult> Create([FromBody] CreateReviewCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdReview = await _mediator.Send(command);
                return CreatedAtAction(nameof(Get), new { id = createdReview.Id }, createdReview);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized review creation: {Message}", ex.Message);
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid review data: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing review
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            command.Id = id;

            try
            {
                var updatedReview = await _mediator.Send(command);
                return Ok(updatedReview);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized review update: {Message}", ex.Message);
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Review not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid review data: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Mark review as helpful
        /// </summary>
        [HttpPut("{id}/helpful")]
        public async Task<IActionResult> MarkHelpful(int id)
        {
            try
            {
                var command = new MarkReviewHelpfulCommand { Id = id };
                var review = await _mediator.Send(command);
                return Ok(new { success = true, review });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Review not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a review
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var command = new DeleteReviewCommand { Id = id };
                await _mediator.Send(command);
                return Ok(new { success = true, message = "Review deleted" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized review deletion: {Message}", ex.Message);
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Review not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
