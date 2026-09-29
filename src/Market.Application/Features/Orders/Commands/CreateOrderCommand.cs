using MediatR;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Orders.Commands
{
    /// <summary>
    /// Create order command.
    /// Only client-owned inputs are accepted: customer, items (product + quantity),
    /// shipping address, and optional notes.
    /// All financial values (unit price, subtotal, tax, shipping cost, total) are
    /// resolved and calculated server-side.
    /// </summary>
    public class CreateOrderCommand : IRequest<OrderResponse>
    {
        public int CustomerId { get; set; }
        public List<OrderItemInput> Items { get; set; } = new();
        public string ShippingAddress { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Order item input — only the product identity and quantity are accepted from the client.
    /// Unit price, vendor, and product name are resolved server-side from the product record.
    /// </summary>
    public class OrderItemInput
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Create order command handler.
    /// Calculates all financial values from authoritative server-side data.
    /// </summary>
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
    {
        // Business rules for order pricing — applied server-side, never trusted from the client.
        private const decimal TaxRate = 0.08m;           // 8 % flat tax on subtotal
        private const decimal FlatShippingCost = 5.00m;  // flat shipping fee per order

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateOrderCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling CreateOrderCommand for customer: {CustomerId}", request.CustomerId);

            if (request.CustomerId <= 0 || !await _unitOfWork.Users.ExistsAsync(request.CustomerId, cancellationToken))
                throw new KeyNotFoundException($"Customer with ID {request.CustomerId} not found");

            if (request.Items.Count == 0)
                throw new ArgumentException("Order must contain at least one item.", nameof(request.Items));

            if (string.IsNullOrWhiteSpace(request.ShippingAddress) || request.ShippingAddress.Length < 10)
                throw new ArgumentException("Shipping address must be at least 10 characters.", nameof(request.ShippingAddress));

            var items = new List<OrderItem>();
            foreach (var input in request.Items)
            {
                if (input.ProductId <= 0 || input.Quantity <= 0)
                    throw new ArgumentException("Each order item must have a valid product ID and positive quantity.");

                var product = await _unitOfWork.Products.GetByIdAsync(input.ProductId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Product with ID {input.ProductId} not found");

                if (!product.IsInStock || product.Quantity < input.Quantity)
                    throw new InvalidOperationException($"Product with ID {input.ProductId} does not have sufficient stock.");

                // Unit price is always the server-side product price — discount applied when available.
                var unitPrice = product.DiscountPrice ?? product.Price;

                product.Quantity -= input.Quantity;
                product.Sold += input.Quantity;
                await _unitOfWork.Products.UpdateAsync(product, cancellationToken);

                items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,    // resolved from the product record
                    VendorId = product.VendorId,   // resolved from the product record
                    Price = unitPrice,             // server-side price, not client-supplied
                    Quantity = input.Quantity
                });
            }

            // CalculateTotal() sets SubTotal = sum of item subtotals and
            // TotalPrice = SubTotal + ShippingCost + Tax.
            var order = new Order
            {
                CustomerId = request.CustomerId,
                OrderNumber = Order.GenerateOrderNumber(),
                Items = items,
                ShippingCost = FlatShippingCost,                          // server-side constant
                Tax = Math.Round(items.Sum(i => i.SubTotal) * TaxRate, 2), // server-side calculation
                ShippingAddress = request.ShippingAddress,
                Notes = request.Notes,
                OrderStatus = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending
            };

            order.CalculateTotal();

            await _unitOfWork.Orders.CreateAsync(order, cancellationToken);
            
            // SaveAsync wraps SaveChangesAsync and catches DbUpdateConcurrencyException.
            // If concurrent orders modified the same product, UnitOfWork will throw InvalidOperationException.
            // We let this exception propagate to the API layer (already handled by controller).
            // The entire transaction is rolled back: Order NOT created, inventory NOT decremented.
            await _unitOfWork.SaveAsync(cancellationToken);

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
