using MediatR;
using Market.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Orders.Commands
{
    /// <summary>
    /// Update order payment status command
    /// </summary>
    public class UpdateOrderPaymentStatusCommand : IRequest<OrderResponse>
    {
        public int Id { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }

    /// <summary>
    /// Update order payment status command handler
    /// </summary>
    public class UpdateOrderPaymentStatusCommandHandler : IRequestHandler<UpdateOrderPaymentStatusCommand, OrderResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateOrderPaymentStatusCommandHandler> _logger;

        public UpdateOrderPaymentStatusCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateOrderPaymentStatusCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<OrderResponse> Handle(UpdateOrderPaymentStatusCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating order {OrderId} payment status to {PaymentStatus}", request.Id, request.PaymentStatus);

            var order = await _unitOfWork.Orders.GetByIdAsync(request.Id, cancellationToken);
            if (order == null)
                throw new KeyNotFoundException($"Order with ID {request.Id} not found");

            // Validate state transition before mutating
            order.ValidatePaymentStatusTransition(request.PaymentStatus);

            order.PaymentStatus = request.PaymentStatus;
            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return MapToResponse(order);
        }

        private OrderResponse MapToResponse(Market.Domain.Entities.Order order)
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
                UpdatedAt = order.UpdatedAt
            };
        }
    }
}



