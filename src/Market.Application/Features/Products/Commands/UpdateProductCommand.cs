using MediatR;
using Microsoft.Extensions.Logging;
using Market.Domain.Entities;
using Market.Application.Features.Products;

namespace Market.Application.Features.Products.Commands
{
    /// <summary>
    /// Update product command
    /// </summary>
    public class UpdateProductCommand : IRequest<ProductResponse>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? SKU { get; set; }
    }

    /// <summary>
    /// Update product command handler
    /// </summary>
    public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductResponse>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UpdateProductCommandHandler> _logger;

        public UpdateProductCommandHandler(IMediator mediator, ILogger<UpdateProductCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<ProductResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UpdateProductCommand for product: {ProductId}", request.Id);

            var result = await _mediator.Send(
                new UpdateProductInternalCommand
                {
                    Id = request.Id,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    DiscountPrice = request.DiscountPrice,
                    Quantity = request.Quantity,
                    ImageUrl = request.ImageUrl,
                    SKU = request.SKU
                },
                cancellationToken);

            return result;
        }
    }

    /// <summary>
    /// Internal command for updating product
    /// </summary>
    internal class UpdateProductInternalCommand : IRequest<ProductResponse>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? SKU { get; set; }
    }

    /// <summary>
    /// Handler for updating a product with SKU uniqueness checks and Vendor state validation
    /// </summary>
    internal class UpdateProductInternalCommandHandler : IRequestHandler<UpdateProductInternalCommand, ProductResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateProductInternalCommandHandler> _logger;

        public UpdateProductInternalCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateProductInternalCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ProductResponse> Handle(UpdateProductInternalCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating product {ProductId}", request.Id);

            // Load existing product
            var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
            if (product == null)
                throw new KeyNotFoundException($"Product {request.Id} not found");

            // Verify vendor is still approved and active
            var vendor = await _unitOfWork.Vendors.GetByIdAsync(product.VendorId, cancellationToken);
            if (vendor == null)
                throw new KeyNotFoundException($"Vendor with ID {product.VendorId} not found");
            
            if (!vendor.IsApproved)
                throw new InvalidOperationException($"Vendor with ID {vendor.Id} is not approved. Only approved vendors can update products.");
            
            if (!vendor.IsActive)
                throw new InvalidOperationException($"Vendor with ID {vendor.Id} is inactive. Only active vendors can update products.");

            // SKU uniqueness check (but allow same SKU on same product)
            if (!string.IsNullOrWhiteSpace(request.SKU))
            {
                var existing = await _unitOfWork.Products.GetBySkuAsync(request.SKU, cancellationToken);
                if (existing != null && existing.Id != product.Id)
                {
                    // Different product already has this SKU
                    throw new InvalidOperationException("A product with this SKU already exists.");
                }
            }

            // Validate input
            var newProduct = new Product
            {
                Id = product.Id,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                DiscountPrice = request.DiscountPrice,
                Quantity = request.Quantity,
                ImageUrl = request.ImageUrl,
                SKU = request.SKU,
                VendorId = product.VendorId,  // Cannot change vendor
                CategoryId = product.CategoryId,  // Cannot change category
                Status = product.Status,
                Sold = product.Sold,
                AverageRating = product.AverageRating,
                ReviewCount = product.ReviewCount
            };

            var validation = new Validators.ProductValidator().Validate(newProduct);
            if (!validation.IsValid)
                throw new ArgumentException(string.Join(" ", validation.Errors.Select(error => error.Message)));

            // Apply updates to existing entity (preserves navigation properties, etc.)
            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.DiscountPrice = request.DiscountPrice;
            product.Quantity = request.Quantity;
            product.ImageUrl = request.ImageUrl;
            product.SKU = request.SKU;

            await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);

            return new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                ImageUrl = product.ImageUrl,
                Quantity = product.Quantity,
                Sold = product.Sold,
                CategoryId = product.CategoryId,
                VendorId = product.VendorId,
                AverageRating = product.AverageRating,
                ReviewCount = product.ReviewCount
            };
        }
    }
}
