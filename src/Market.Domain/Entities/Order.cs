using Market.Domain.Common;
using Market.Domain.Enums;

namespace Market.Domain.Entities
{
    /// <summary>
    /// Order entity for e-commerce platform
    /// </summary>
    public class Order : BaseEntity
    {
        public int CustomerId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalPrice { get; set; }

        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public string ShippingAddress { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        public User Customer { get; set; } = null!;
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        /// <summary>
        /// Generate unique order number
        /// </summary>
        public static string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        /// <summary>
        /// Calculate total price
        /// </summary>
        public void CalculateTotal()
        {
            SubTotal = Items.Sum(x => x.SubTotal);
            TotalPrice = SubTotal + ShippingCost + Tax;
        }

        /// <summary>
        /// Validate if a transition to a new OrderStatus is allowed.
        /// Implements server-side state machine rules to prevent invalid transitions.
        /// Throws InvalidOperationException if transition is not allowed.
        /// </summary>
        public void ValidateOrderStatusTransition(OrderStatus newStatus)
        {
            // No-op: same state is always allowed
            if (newStatus == OrderStatus)
                return;

            // Check if transition is valid based on current state
            var isValid = (OrderStatus) switch
            {
                // Pending: can go to Confirmed or Cancelled
                OrderStatus.Pending => newStatus == OrderStatus.Confirmed || newStatus == OrderStatus.Cancelled,

                // Confirmed: can go to Processing or Cancelled
                OrderStatus.Confirmed => newStatus == OrderStatus.Processing || newStatus == OrderStatus.Cancelled,

                // Processing: can go to Shipped or Cancelled
                OrderStatus.Processing => newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Cancelled,

                // Shipped: can only go to Delivered
                OrderStatus.Shipped => newStatus == OrderStatus.Delivered,

                // Terminal states: cannot transition
                OrderStatus.Delivered => false,
                OrderStatus.Cancelled => false,
                OrderStatus.Refunded => false,

                _ => false
            };

            if (!isValid)
                throw new InvalidOperationException(
                    $"Cannot transition order status from {OrderStatus} to {newStatus}. Invalid state transition.");
        }

        /// <summary>
        /// Validate if a transition to a new PaymentStatus is allowed.
        /// Implements server-side payment state rules to prevent invalid transitions.
        /// Throws InvalidOperationException if transition is not allowed.
        /// </summary>
        public void ValidatePaymentStatusTransition(PaymentStatus newStatus)
        {
            // No-op: same state is always allowed
            if (newStatus == PaymentStatus)
                return;

            // Check if transition is valid based on current state
            var isValid = (PaymentStatus) switch
            {
                // Pending: can go to Completed, Failed, or Cancelled
                PaymentStatus.Pending => newStatus == PaymentStatus.Completed || 
                                         newStatus == PaymentStatus.Failed || 
                                         newStatus == PaymentStatus.Cancelled,

                // Completed: can go to Refunded
                PaymentStatus.Completed => newStatus == PaymentStatus.Refunded,

                // Terminal states: cannot transition
                PaymentStatus.Failed => false,
                PaymentStatus.Cancelled => false,
                PaymentStatus.Refunded => false,

                _ => false
            };

            if (!isValid)
                throw new InvalidOperationException(
                    $"Cannot transition payment status from {PaymentStatus} to {newStatus}. Invalid state transition.");
        }

        /// <summary>
        /// Helper method: returns true if a transition to newStatus is allowed, false otherwise.
        /// Used for pre-validation checks (e.g., in tests or conditions).
        /// Does NOT throw exceptions.
        /// </summary>
        public bool CanTransitionTo(OrderStatus newStatus)
        {
            try
            {
                ValidateOrderStatusTransition(newStatus);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Helper method: returns true if a transition to newStatus is allowed, false otherwise.
        /// Used for pre-validation checks (e.g., in tests or conditions).
        /// Does NOT throw exceptions.
        /// </summary>
        public bool CanTransitionPaymentTo(PaymentStatus newStatus)
        {
            try
            {
                ValidatePaymentStatusTransition(newStatus);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Order item - product in an order
    /// </summary>
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int VendorId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal => Price * Quantity;

        // Navigation properties
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public Vendor Vendor { get; set; } = null!;
    }
}

