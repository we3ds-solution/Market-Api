# Implementation Guide

Implementation details for Market API v3.0+ with Clean Architecture, CQRS, EF Core, and SQL Server.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Entity Structure](#entity-structure)
- [MediatR Commands & Queries](#mediatr-commands--queries)
- [Validation Framework](#validation-framework)
- [Repository Pattern](#repository-pattern)
- [Unit of Work](#unit-of-work)
- [Dependency Injection](#dependency-injection)
- [EF Core Configuration](#ef-core-configuration)

## Architecture Overview

Market API follows a **strict 4-layer Clean Architecture** with **CQRS (Command Query Responsibility Segregation)** using MediatR:

```
┌─────────────────────────────┐
│   API Layer                 │ ← Controllers, Middleware, HTTP
├─────────────────────────────┤
│   Application Layer         │ ← Commands, Queries, Handlers, DTOs
├─────────────────────────────┤
│   Infrastructure Layer      │ ← DbContext, Repositories, EF Core
├─────────────────────────────┤
│   Domain Layer              │ ← Entities, Value Objects, Interfaces
└─────────────────────────────┘
```

## Entity Structure

All entities inherit from `BaseEntity` which includes:

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
```

### Core Entities

1. **Product** - Marketplace products with pricing, inventory, ratings, soft-delete support
2. **Category** - Product categorization with slug, active status, and display order
3. **User** - Customer/vendor accounts with roles
4. **Vendor** - Store information with approval status and commission
5. **Order** - Customer orders with items, payment, and shipping (immutable snapshots)
6. **OrderItem** - Order line items with historical pricing data
7. **Cart** - Shopping carts with line items (mutable, per-user)
8. **Review** - Product reviews with ratings, verified-purchase checks, and duplication prevention

All entities are configured via EF Core FluentAPI in `Infrastructure/Data/EntityConfigurations/`.

## MediatR Commands & Queries

### Command Pattern (Write Operations)

Commands represent **state-changing operations** (Create, Update, Delete).

**Example - Create Product Command**:
```csharp
// 1. Define the Command (Request)
public class CreateProductCommand : IRequest<ProductResponse>
{
    public string Name { get; set; }
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
}

// 2. Implement the Handler
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
        _logger.LogInformation("Creating product: {Name}", request.Name);
        
        // Validate category exists and is active
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null || !category.IsActive)
            throw new InvalidOperationException($"Category {request.CategoryId} not found or inactive");
        
        var product = new Product 
        { 
            Name = request.Name, 
            CategoryId = request.CategoryId,
            Price = request.Price 
        };
        
        await _unitOfWork.Products.CreateAsync(product, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        
        return new ProductResponse { Id = product.Id, Name = product.Name };
    }
}

// 3. Use in Controller
[HttpPost]
public async Task<ActionResult<ProductResponse>> CreateProduct(CreateProductCommand command)
{
    var result = await _mediator.Send(command);
    return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
}
```

### Query Pattern (Read Operations)

Queries represent **read-only operations**. They do not modify state.

**Example - Get Product By ID Query**:
```csharp
// 1. Define the Query (Request)
public class GetProductByIdQuery : IRequest<ProductResponse?>
{
    public int Id { get; set; }
}

// 2. Implement the Handler
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductResponse?>
{
    private readonly IProductReadService _productReadService;
    
    public GetProductByIdQueryHandler(IProductReadService productReadService)
    {
        _productReadService = productReadService;
    }
    
    public async Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return await _productReadService.GetByIdAsync(request.Id, cancellationToken);
    }
}

// 3. Use in Controller
[HttpGet("{id}")]
public async Task<ActionResult<ProductResponse>> GetProduct(int id)
{
    var result = await _mediator.Send(new GetProductByIdQuery { Id = id });
    if (result == null)
        return NotFound();
    return Ok(result);
}
```

## Validation Framework

Market API uses **FluentValidation** for declarative validation rules.

**Product Validator**:
```csharp
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(255).WithMessage("Product name cannot exceed 255 characters");

        RuleFor(p => p.Price)
            .GreaterThan(0).WithMessage("Product price must be greater than 0");

        RuleFor(p => p.CategoryId)
            .GreaterThan(0).WithMessage("Category ID must be valid");
    }
}
```

**Validator Pipeline Behavior** (MediatR middleware):
```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

## Repository Pattern

The Repository pattern abstracts data access behind domain interfaces.

**Domain Interface** (`Market.Domain/Interfaces/IProductRepository.cs`):
```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task CreateAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
}
```

**Infrastructure Implementation** (`Market.Infrastructure/Persistence/ProductRepository.cs`):
```csharp
public class ProductRepository : IProductRepository
{
    private readonly MarketDbContext _dbContext;
    
    public ProductRepository(MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
    
    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Where(p => !p.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SKU == sku && !p.IsDeleted, cancellationToken);
    }
    
    public async Task CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }
    
    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Update(product);
        await Task.CompletedTask;
    }
    
    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.IsDeleted = true;  // Soft delete
        _dbContext.Products.Update(product);
        await Task.CompletedTask;
    }
}
```

**Key Points**:
- Always query with `Where(p => !p.IsDeleted)` for soft-deleted entities
- Use `AsNoTracking()` for read-only queries (better performance)
- Repository is thin—mostly delegates to EF Core
- All async operations support cancellation tokens

## Unit of Work

**Domain Interface** (`Market.Domain/Interfaces/IUnitOfWork.cs`):
```csharp
public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IUserRepository Users { get; }
    IVendorRepository Vendors { get; }
    IOrderRepository Orders { get; }
    ICartRepository Carts { get; }
    IReviewRepository Reviews { get; }
    
    Task<int> SaveAsync(CancellationToken cancellationToken = default);
}
```

**Infrastructure Implementation** (`Market.Infrastructure/Persistence/UnitOfWork.cs`):
```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly MarketDbContext _dbContext;
    private readonly ILogger<UnitOfWork> _logger;

    private IProductRepository? _productRepository;
    private ICategoryRepository? _categoryRepository;
    // ... other repositories
    
    public UnitOfWork(MarketDbContext dbContext, ILogger<UnitOfWork> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public IProductRepository Products =>
        _productRepository ??= new ProductRepository(_dbContext);
    
    public ICategoryRepository Categories =>
        _categoryRepository ??= new CategoryRepository(_dbContext);
    
    // ... other repository properties
    
    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving changes to database");
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
```

**Advantages**:
- Single save point across multiple repositories
- Atomic transactions (all or nothing)
- Lazy initialization of repositories
- Centralized DbContext management

## Dependency Injection

**Application Registration** (`Program.cs`):
```csharp
// Add application services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
// ... other repositories

// Add database
builder.Services.AddDbContext<MarketDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add Validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Add validation behavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Add logging
builder.Services.AddLogging();
```

## EF Core Configuration

All entity configurations are in `Infrastructure/Data/EntityConfigurations/`.

**Example - Product Configuration**:
```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Primary key
        builder.HasKey(p => p.Id);

        // Properties with constraints
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        // Indexes
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.VendorId);
        builder.HasIndex(p => p.SKU)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [SKU] IS NOT NULL");

        // Relationships
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Vendor)
            .WithMany(v => v.Products)
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Table
        builder.ToTable("Products");
    }
}
```

**Key Configuration Points**:
- Use `IsRequired()` for NOT NULL columns
- Use `HasMaxLength()` for VARCHAR columns
- Use `HasPrecision()` for decimal/float precision
- Use `IsUnique()` for unique constraints
- Use `OnDelete(DeleteBehavior.Restrict)` to prevent cascade deletes
- Always configure indexes for foreign keys

## Best Practices

1. **Always use soft-delete filters** - Don't expose deleted entities to business logic
2. **Use cancellation tokens** - Support graceful cancellation of long-running queries
3. **Use AsNoTracking() for reads** - Better performance when you don't need change tracking
4. **Keep repositories thin** - Most logic goes in handlers, not repositories
5. **Use value objects** - Slug, Money, Email encapsulate validation
6. **Validate early** - Catch errors in handlers before hitting the database
7. **Log operations** - Important for debugging and auditing
8. **Handle exceptions** - Return meaningful error responses
