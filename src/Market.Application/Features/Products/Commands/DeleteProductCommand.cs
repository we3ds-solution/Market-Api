using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Products.Commands
{
    /// <summary>
    /// Delete product command
    /// </summary>
    public class DeleteProductCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Delete product command handler - implements soft-delete pattern.
    /// Soft-delete: sets IsDeleted=true and DeletedAt=UtcNow.
    /// Historical OrderItems, CartItems, and Reviews remain intact with FK Restrict behavior.
    /// Deleted products are automatically filtered from all read queries.
    /// 
    /// NOTE: Previous version used nested MediatR dispatch to internal handler (anti-pattern).
    /// Refactored to consolidate into single handler for clarity and proper handler responsibility.
    /// </summary>
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteProductCommandHandler> _logger;

        public DeleteProductCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteProductCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling DeleteProductCommand for product: {ProductId}", request.Id);

            // Load the product (soft-deleted products are excluded by default)
            var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
            if (product == null)
                throw new KeyNotFoundException($"Product with ID {request.Id} not found");

            // Soft-delete the product: sets IsDeleted=true, DeletedAt=UtcNow
            product.Delete();

            // Persist the deletion
            await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            _logger.LogInformation("Product {ProductId} successfully soft-deleted", request.Id);
            return true;
        }
    }
}



