using Market.Domain.Enums;
using Market.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Orders.Commands
{
    public class CancelOrderCommand : IRequest<bool>
    {
        public int Id { get; set; }
        public string? Reason { get; set; }
    }

    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CancelOrderCommandHandler> _logger;

        public CancelOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<CancelOrderCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling CancelOrderCommand for order: {Id}", request.Id);

            var order = await _unitOfWork.Orders.GetByIdAsync(request.Id, cancellationToken);
            if (order == null)
                return false;

            // Validate state transition to Cancelled
            order.ValidateOrderStatusTransition(OrderStatus.Cancelled);

            // Update order status
            order.OrderStatus = OrderStatus.Cancelled;
            
            // Restore inventory for all items
            foreach (var item in order.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
                if (product != null)
                {
                    product.Quantity += item.Quantity;
                    product.Sold -= item.Quantity;
                    await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
                }
            }

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return true;
        }
    }
}



