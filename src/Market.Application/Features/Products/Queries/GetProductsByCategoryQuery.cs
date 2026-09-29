using MediatR;
using Market.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Products.Queries
{
    /// <summary>
    /// Get products by category query
    /// </summary>
    public class GetProductsByCategoryQuery : IRequest<List<ProductResponse>>
    {
        public int CategoryId { get; set; }
    }

    /// <summary>
    /// Get products by category query handler
    /// Verifies the category exists and is active before returning products.
    /// </summary>
    public class GetProductsByCategoryQueryHandler : IRequestHandler<GetProductsByCategoryQuery, List<ProductResponse>>
    {
        private readonly IProductReadService _productReadService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetProductsByCategoryQueryHandler> _logger;

        public GetProductsByCategoryQueryHandler(IProductReadService productReadService, IUnitOfWork unitOfWork, ILogger<GetProductsByCategoryQueryHandler> logger)
        {
            _productReadService = productReadService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<ProductResponse>> Handle(GetProductsByCategoryQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetProductsByCategoryQuery for category: {CategoryId}", request.CategoryId);

            // Verify category exists
            var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found", request.CategoryId);
                return new List<ProductResponse>();
            }

            // Verify category is active
            if (!category.IsActive)
            {
                _logger.LogWarning("Category with ID {CategoryId} is inactive", request.CategoryId);
                return new List<ProductResponse>();
            }

            var products = await _productReadService.GetProductsByCategoryAsync(request.CategoryId, cancellationToken);
            return products.ToList();
        }
    }
}



