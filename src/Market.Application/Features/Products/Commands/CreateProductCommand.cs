using MediatR;
using Market.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Market.Application.Features.Products.Commands
{
    /// <summary>
    /// Create product command
    /// </summary>
    public class CreateProductCommand : IRequest<ProductResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? SKU { get; set; }
    }

    /// <summary>
    /// Create product command handler
    /// </summary>
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateProductCommandHandler> _logger;

        public CreateProductCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateProductCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling CreateProductCommand for product: {ProductName}", request.Name);

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                VendorId = request.VendorId,
                CategoryId = request.CategoryId,
                Price = request.Price,
                DiscountPrice = request.DiscountPrice,
                Quantity = request.Quantity,
                ImageUrl = request.ImageUrl,
                SKU = request.SKU
            };

            var validation = new Validators.ProductValidator().Validate(product);
            if (!validation.IsValid)
                throw new ArgumentException(string.Join(" ", validation.Errors.Select(error => error.Message)));

            // Verify vendor exists and is approved & active
            var vendor = await _unitOfWork.Vendors.GetByIdAsync(product.VendorId, cancellationToken);
            if (vendor == null)
                throw new KeyNotFoundException($"Vendor with ID {product.VendorId} not found");
            
            // Only approved and active vendors can create products
            if (!vendor.IsApproved)
                throw new InvalidOperationException($"Vendor with ID {vendor.Id} is not approved. Only approved vendors can create products.");
            
            if (!vendor.IsActive)
                throw new InvalidOperationException($"Vendor with ID {vendor.Id} is inactive. Only active vendors can create products.");

            if (!await _unitOfWork.Categories.ExistsAsync(product.CategoryId, cancellationToken))
                throw new KeyNotFoundException($"Category with ID {product.CategoryId} not found");
            if (!string.IsNullOrWhiteSpace(product.SKU) && await _unitOfWork.Products.GetBySkuAsync(product.SKU, cancellationToken) != null)
                throw new InvalidOperationException("A product with this SKU already exists.");

            await _unitOfWork.Products.CreateAsync(product, cancellationToken);
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


