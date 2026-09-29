using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Categories.Commands
{
    /// <summary>
    /// Update category command
    /// </summary>
    public class UpdateCategoryCommand : IRequest<CategoryResponse>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Update category command handler
    /// </summary>
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryResponse>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UpdateCategoryCommandHandler> _logger;

        public UpdateCategoryCommandHandler(IMediator mediator, ILogger<UpdateCategoryCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<CategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UpdateCategoryCommand for category: {CategoryId}", request.Id);

            var result = await _mediator.Send(
                new UpdateCategoryInternalCommand
                {
                    Id = request.Id,
                    Name = request.Name,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl
                },
                cancellationToken);

            return result;
        }
    }

    /// <summary>
    /// Internal command for updating category
    /// </summary>
    internal class UpdateCategoryInternalCommand : IRequest<CategoryResponse>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Handler for updating category with slug regeneration
    /// </summary>
    internal class UpdateCategoryInternalCommandHandler : IRequestHandler<UpdateCategoryInternalCommand, CategoryResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateCategoryInternalCommandHandler> _logger;

        public UpdateCategoryInternalCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateCategoryInternalCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CategoryResponse> Handle(UpdateCategoryInternalCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating category {CategoryId}", request.Id);

            // Load existing category
            var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
            if (category == null)
                throw new KeyNotFoundException($"Category with ID {request.Id} not found");

            // Update properties
            category.Name = request.Name;
            category.Description = request.Description;
            category.ImageUrl = request.ImageUrl;

            // Regenerate slug from the updated name
            category.UpdateSlugFromName();

            // Persist changes
            await _unitOfWork.Categories.UpdateAsync(category, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation("Category {CategoryId} successfully updated", request.Id);

            return new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                Slug = category.Slug,
                ParentCategoryId = category.ParentCategoryId,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }
    }
}



