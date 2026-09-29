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
    /// Delete product command handler
    /// </summary>
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DeleteProductCommandHandler> _logger;

        public DeleteProductCommandHandler(IMediator mediator, ILogger<DeleteProductCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling DeleteProductCommand for product: {ProductId}", request.Id);

            var result = await _mediator.Send(new DeleteProductInternalCommand { Id = request.Id }, cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Internal command for deleting product
    /// </summary>
    internal class DeleteProductInternalCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Internal command handler for deleting a product.
    /// Implements soft-delete: sets IsDeleted=true and DeletedAt=UtcNow.
    /// Historical OrderItems, CartItems, and Reviews remain intact with FK Restrict behavior.
    /// Deleted products are automatically filtered from all read queries.
    /// </summary>
    internal class DeleteProductInternalCommandHandler : IRequestHandler<DeleteProductInternalCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteProductInternalCommandHandler> _logger;

        public DeleteProductInternalCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteProductInternalCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteProductInternalCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling DeleteProductInternalCommand for product: {ProductId}", request.Id);

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



