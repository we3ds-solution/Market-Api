using MediatR;
using Market.Application.Features.Products.Commands;
using Market.Application.Features.Products.Queries;

namespace Market.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var query = new GetAllProductsQuery();
            var products = await _mediator.Send(query);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var query = new GetProductByIdQuery { Id = id };
            var product = await _mediator.Send(query);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // [HttpGet("GetByPriceRange/{minPrice}/{maxPrice}")]
        // public async Task<IActionResult> GetByPriceRange(decimal minPrice, decimal maxPrice)
        // {
        //     var query = new GetProductsByPriceRangeQuery { MinPrice = minPrice, MaxPrice = maxPrice };
        //     var products = await _mediator.Send(query);
        //     return Ok(products);
        // }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdProduct = await _mediator.Send(command);
                return CreatedAtAction(nameof(Get), new { id = createdProduct.Id }, createdProduct);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("SKU"))
            {
                _logger.LogWarning("SKU conflict on create: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid product data: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateProductCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            command.Id = id;

            try
            {
                var updatedProduct = await _mediator.Send(command);
                return Ok(updatedProduct);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("SKU conflict on update: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Product not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid product data: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var command = new DeleteProductCommand { Id = id };
                await _mediator.Send(command);
                return Ok(new { success = true, message = "Product deleted" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Product not found: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

