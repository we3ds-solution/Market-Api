using Market.Domain.Repositories;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Market.Infrastructure.Data;

namespace Market.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Order repository implementation for EF Core
    /// </summary>
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(MarketDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get order by order number
        /// </summary>
        public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(x => x.OrderNumber == orderNumber && !x.IsDeleted, cancellationToken);
        }

        /// <summary>
        /// Get orders by customer ID
        /// </summary>
        public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(x => x.CustomerId == customerId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get orders by status
        /// </summary>
        public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(x => x.OrderStatus == status && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get orders by payment status
        /// </summary>
        public async Task<IEnumerable<Order>> GetByPaymentStatusAsync(PaymentStatus paymentStatus, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(x => x.PaymentStatus == paymentStatus && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get pending orders
        /// </summary>
        public async Task<IEnumerable<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(x => x.OrderStatus == OrderStatus.Pending && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get recent orders
        /// </summary>
        public async Task<IEnumerable<Order>> GetRecentOrdersAsync(int count = 50, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Check if customer has a verified purchase of a specific product
        /// </summary>
        public async Task<bool> CustomerHasVerifiedPurchaseOfProductAsync(
            int customerId,
            int productId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(o => o.CustomerId == customerId &&
                            !o.IsDeleted &&
                            o.OrderStatus == OrderStatus.Delivered &&
                            o.PaymentStatus == PaymentStatus.Completed)
                .AnyAsync(o => o.Items.Any(item => item.ProductId == productId),
                          cancellationToken);
        }
    }
}



