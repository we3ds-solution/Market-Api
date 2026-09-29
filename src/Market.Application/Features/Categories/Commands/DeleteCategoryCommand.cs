using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Categories.Commands
{
    /// <summary>
    /// Delete category command
    /// </summary>
    public class DeleteCategoryCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Delete category command handler
    /// </summary>
    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DeleteCategoryCommandHandler> _logger;

        public DeleteCategoryCommandHandler(IMediator mediator, ILogger<DeleteCategoryCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling DeleteCategoryCommand for category: {CategoryId}", request.Id);

            var result = await _mediator.Send(new DeleteCategoryInternalCommand { Id = request.Id }, cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Internal command for deleting category
    /// </summary>
    internal class DeleteCategoryInternalCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Handler for deleting category with referential integrity check.
    /// EF Core FK constraint (DeleteBehavior.Restrict) prevents deletion if Products reference the Category.
    /// Soft-delete is used: sets IsDeleted=true and DeletedAt=UtcNow.
    /// </summary>
    internal class DeleteCategoryInternalCommandHandler : IRequestHandler<DeleteCategoryInternalCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteCategoryInternalCommandHandler> _logger;

        public DeleteCategoryInternalCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteCategoryInternalCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteCategoryInternalCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling DeleteCategoryInternalCommand for category: {CategoryId}", request.Id);

            // Load the category (soft-deleted categories are excluded by default)
            var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
            if (category == null)
                throw new KeyNotFoundException($"Category with ID {request.Id} not found");

            // Soft-delete the category: sets IsDeleted=true, DeletedAt=UtcNow
            category.Delete();

            // Persist the deletion
            await _unitOfWork.Categories.UpdateAsync(category, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation("Category {CategoryId} successfully soft-deleted", request.Id);
            return true;
        }
    }
}



