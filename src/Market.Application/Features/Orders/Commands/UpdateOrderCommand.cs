using Market.Domain.Enums;
using Market.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Orders.Commands
{
    /// <summary>
    /// Update order command
    /// </summary>
    public class UpdateOrderCommand : IRequest<OrderResponse>
    {
        public int Id { get; set; }
        public OrderStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Update order command handler
    /// </summary>
    public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, OrderResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateOrderCommandHandler> _logger;

        public UpdateOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateOrderCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<OrderResponse> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UpdateOrderCommand for order: {OrderId}", request.Id);

            var order = await _unitOfWork.Orders.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Order with ID {request.Id} not found");

            // Update order status with validation
            if (request.Status.HasValue)
                order.ValidateOrderStatusTransition(request.Status.Value);

            // Update allowed fields
            if (request.Status.HasValue)
                order.OrderStatus = request.Status.Value;

            if (request.PaymentStatus.HasValue)
                order.ValidatePaymentStatusTransition(request.PaymentStatus.Value);

            if (request.PaymentStatus.HasValue)
                order.PaymentStatus = request.PaymentStatus.Value;

            if (!string.IsNullOrEmpty(request.TrackingNumber))
                order.TrackingNumber = request.TrackingNumber;

            if (!string.IsNullOrEmpty(request.Notes))
                order.Notes = request.Notes;

            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return MapToResponse(order);
        }

        private OrderResponse MapToResponse(Domain.Entities.Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                OrderNumber = order.OrderNumber,
                SubTotal = order.SubTotal,
                ShippingCost = order.ShippingCost,
                Tax = order.Tax,
                TotalPrice = order.TotalPrice,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                ShippingAddress = order.ShippingAddress,
                TrackingNumber = order.TrackingNumber,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(item => new OrderItemResponse
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    VendorId = item.VendorId,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    SubTotal = item.SubTotal
                }).ToList()
            };
        }
    }
}



