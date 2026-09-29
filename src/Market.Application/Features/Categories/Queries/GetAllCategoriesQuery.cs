using MediatR;
using Market.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Categories.Queries
{
    /// <summary>
    /// Get all categories query
    /// </summary>
    public class GetAllCategoriesQuery : IRequest<List<CategoryResponse>>
    {
    }

    /// <summary>
    /// Get all categories query handler
    /// </summary>
    public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, List<CategoryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllCategoriesQueryHandler> _logger;

        public GetAllCategoriesQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAllCategoriesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<CategoryResponse>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetAllCategoriesQuery");

            var categories = await _unitOfWork.Categories.GetAllAsync();
            
            // Filter to active categories only for client queries
            return categories
                .Where(c => c.IsActive)
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    Slug = c.Slug,
                    ParentCategoryId = c.ParentCategoryId,
                    IsActive = c.IsActive,
                    DisplayOrder = c.DisplayOrder,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToList();
        }
    }
}



