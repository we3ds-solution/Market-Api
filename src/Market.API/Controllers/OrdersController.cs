using MediatR;
using Market.Domain.Enums;
using Market.Application.Features.Orders.Commands;
using Market.Application.Features.Orders.Queries;

namespace Market.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get all orders
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var query = new GetAllOrdersQuery();
            var orders = await _mediator.Send(query);
            return Ok(orders);
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var query = new GetOrderByIdQuery { Id = id };
            var order = await _mediator.Send(query);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        /// <summary>
        /// Get order by order number
        /// </summary>
        [HttpGet("number/{orderNumber}")]
        public async Task<IActionResult> GetByOrderNumber(string orderNumber)
        {
            var query = new GetOrderByNumberQuery { OrderNumber = orderNumber };
            var order = await _mediator.Send(query);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        /// <summary>
        /// Get orders by customer ID
        /// </summary>
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var query = new GetOrdersByCustomerQuery { CustomerId = customerId };
            var orders = await _mediator.Send(query);
            return Ok(orders);
        }

        /// <summary>
        /// Get orders by status
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetByStatus(OrderStatus status)
        {
            var query = new GetOrdersByStatusQuery { Status = status };
            var orders = await _mediator.Send(query);
            return Ok(orders);
        }

        /// <summary>
        /// Get orders by payment status
        /// </summary>
        [HttpGet("payment-status/{paymentStatus}")]
        public async Task<IActionResult> GetByPaymentStatus(PaymentStatus paymentStatus)
        {
            var query = new GetOrdersByPaymentStatusQuery { PaymentStatus = paymentStatus };
            var orders = await _mediator.Send(query);
            return Ok(orders);
        }

        /// <summary>
        /// Get pending orders
        /// </summary>
        [HttpGet("pending/list")]
        public async Task<IActionResult> GetPending()
        {
            var query = new GetPendingOrdersQuery();
            var orders = await _mediator.Send(query);
            return Ok(orders);
        }

        /// <summary>
        /// Get recent orders
        /// </summary>
        [HttpGet("recent/list")]
        public async Task<IActionResult> GetRecent([FromQuery] int count = 50)
        {
            var query = new GetRecentOrdersQuery { Count = count };
            var orders = await _mediator.Send(query);
            return Ok(orders);
        }

        /// <summary>
        /// Create a new order
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("write")]
        public async Task<IActionResult> Create([FromBody] CreateOrderCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdOrder = await _mediator.Send(command);
                return CreatedAtAction(nameof(Get), new { id = createdOrder.Id }, createdOrder);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid order data: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Resource not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing order
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            command.Id = id;

            try
            {
                var updatedOrder = await _mediator.Send(command);
                return Ok(updatedOrder);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Order not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cancel an order
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var command = new CancelOrderCommand { Id = id };
                var order = await _mediator.Send(command);
                return Ok(new { success = true, order });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Order not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update order status
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusCommand command)
        {
            try
            {
                command.Id = id;
                var order = await _mediator.Send(command);
                return Ok(new { success = true, order });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Order not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update payment status
        /// </summary>
        [HttpPut("{id}/payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdateOrderPaymentStatusCommand command)
        {
            try
            {
                command.Id = id;
                var order = await _mediator.Send(command);
                return Ok(new { success = true, order });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Order not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update tracking number
        /// </summary>
        [HttpPut("{id}/tracking")]
        public async Task<IActionResult> UpdateTracking(int id, [FromBody] UpdateOrderTrackingCommand command)
        {
            try
            {
                command.Id = id;
                var order = await _mediator.Send(command);
                return Ok(new { success = true, order });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Order not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete an order
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var command = new DeleteOrderCommand { Id = id };
                await _mediator.Send(command);
                return Ok(new { success = true, message = "Order deleted" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Order not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
