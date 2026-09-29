# Project Structure

Complete directory structure and organization of Market API v3.0+ with Clean Architecture.

## Root Directory Structure

```
Market-Api/
├── .git/                          # Git repository
├── .github/                       # GitHub configuration
│   ├── workflows/                 # CI/CD workflows
│   │   ├── docker.yml            # Docker image build
│   │   ├── dotnet.yml            # .NET build and test
│   │   ├── codeql.yml            # Security analysis
│   │   └── dotnet-format.yml     # Code formatting
│   ├── ISSUE_TEMPLATE/           # Issue templates
│   ├── CODE_OF_CONDUCT.md
│   ├── PULL_REQUEST_TEMPLATE.md
│   └── dependabot.yml
├── .gitignore                    # Git ignore rules
├── src/                          # Source code (4 layers)
│   ├── Market.Domain/            # Layer 1: Core domain
│   ├── Market.Application/       # Layer 2: Use cases
│   ├── Market.Infrastructure/    # Layer 3: Data & external services
│   └── Market.API/               # Layer 4: Presentation
├── tests/                        # Test projects
│   ├── Market.Domain.Tests/
│   ├── Market.Application.Tests/
│   ├── Market.Infrastructure.Tests/
│   ├── Market.API.Tests/
│   └── Market.Tests.Common/
├── docs/                         # Documentation
├── .agent/                       # Agent workflow documentation
├── .env.example                  # Environment template
├── docker-compose.yml            # Docker Compose configuration
├── Dockerfile                    # Docker image definition
├── Market.sln                    # Solution file
├── LICENSE                       # MIT License
├── README.md                     # Project overview
└── CHANGELOG.md                  # Version history

```

## Layered Architecture

### 1. Domain Layer (`src/Market.Domain`)

**Responsibility**: Core business logic, no external dependencies.

```
Market.Domain/
├── Entities/                     # Domain entities
│   ├── BaseEntity.cs            # Base class for all entities
│   ├── Product.cs               # Product entity
│   ├── Category.cs              # Category entity
│   ├── User.cs
│   ├── Vendor.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── Cart.cs
│   ├── CartItem.cs
│   ├── Review.cs
│   ├── ReviewImage.cs
│   ├── ProductTag.cs
│   └── Tag.cs
│
├── Enums/                        # Domain enumerations
│   ├── OrderStatus.cs           # Pending, Processing, Shipped, Delivered, Cancelled
│   ├── PaymentStatus.cs         # Pending, Completed, Failed, Refunded
│   ├── UserRole.cs              # Customer, Vendor, Admin
│   ├── ProductStatus.cs         # Active, Inactive, Deleted
│   └── VendorApprovalStatus.cs  # Pending, Approved, Rejected
│
├── ValueObjects/                # Immutable value objects
│   ├── Slug.cs                  # URL-safe category slug
│   ├── Money.cs                 # Price with currency
│   ├── Email.cs                 # Validated email
│   └── Tag.cs                   # Product tag
│
├── Interfaces/                  # Repository contracts
│   ├── IRepository.cs           # Generic repository interface (if needed)
│   ├── IProductRepository.cs
│   ├── ICategoryRepository.cs
│   ├── IUserRepository.cs
│   ├── IVendorRepository.cs
│   ├── IOrderRepository.cs
│   ├── ICartRepository.cs
│   ├── IReviewRepository.cs
│   └── IUnitOfWork.cs           # Unit of Work interface
│
├── Common/                       # Common domain code
│   └── Exceptions/              # Custom exceptions
│       ├── DomainException.cs
│       └── ValidationException.cs
│
└── Market.Domain.csproj         # No external NuGet dependencies

```

### 2. Application Layer (`src/Market.Application`)

**Responsibility**: Use cases, business rules, orchestration. Depends only on Domain.

```
Market.Application/
├── Features/                     # Organized by feature (CQRS)
│   ├── Products/
│   │   ├── Commands/
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── UpdateProductCommand.cs
│   │   │   └── DeleteProductCommand.cs
│   │   ├── Queries/
│   │   │   ├── GetAllProductsQuery.cs
│   │   │   ├── GetProductByIdQuery.cs
│   │   │   └── GetProductsByCategoryQuery.cs
│   │   ├── ProductResponse.cs   # Response DTO
│   │   └── ProductValidator.cs  # FluentValidation rules
│   │
│   ├── Categories/
│   │   ├── Commands/
│   │   │   ├── CreateCategoryCommand.cs
│   │   │   ├── UpdateCategoryCommand.cs
│   │   │   └── DeleteCategoryCommand.cs
│   │   ├── Queries/
│   │   │   ├── GetAllCategoriesQuery.cs
│   │   │   └── GetCategoryByIdQuery.cs
│   │   ├── CategoryResponse.cs
│   │   └── CategoryValidator.cs
│   │
│   ├── Orders/
│   ├── Users/
│   ├── Vendors/
│   ├── Carts/
│   └── Reviews/
│
├── Validators/                   # Shared validators
│   └── (feature-specific validators in each feature folder)
│
├── DTOs/                         # Shared response models
│   └── (most DTOs in feature folders)
│
├── Abstractions/                 # Interfaces and abstractions
│   └── Services/                 # (if needed for cross-feature logic)
│
├── Middleware/                   # MediatR pipeline behaviors
│   ├── ValidationBehavior.cs    # Automatic validation
│   ├── LoggingBehavior.cs       # Request/response logging
│   └── ExceptionHandlingBehavior.cs
│
├── GlobalUsings.cs             # Common using statements
└── Market.Application.csproj   # Dependencies: Domain, FluentValidation, MediatR

```

### 3. Infrastructure Layer (`src/Market.Infrastructure`)

**Responsibility**: Data access, external services, persistence. Implements Domain interfaces.

```
Market.Infrastructure/
├── Data/                         # EF Core database layer
│   ├── MarketDbContext.cs       # DbContext for SQL Server
│   ├── EntityConfigurations/    # FluentAPI configurations
│   │   ├── ProductConfiguration.cs
│   │   ├── CategoryConfiguration.cs
│   │   ├── UserConfiguration.cs
│   │   ├── VendorConfiguration.cs
│   │   ├── OrderConfiguration.cs
│   │   ├── OrderItemConfiguration.cs
│   │   ├── CartConfiguration.cs
│   │   ├── CartItemConfiguration.cs
│   │   ├── ReviewConfiguration.cs
│   │   ├── ReviewImageConfiguration.cs
│   │   ├── ProductTagConfiguration.cs
│   │   └── UserConfiguration.cs
│   │
│   └── Seeds/                   # Initial data seeds
│       └── DataSeeder.cs
│
├── Persistence/                  # Repository implementations
│   ├── Repositories/
│   │   ├── ProductRepository.cs  # Implements IProductRepository
│   │   ├── CategoryRepository.cs
│   │   ├── UserRepository.cs
│   │   ├── VendorRepository.cs
│   │   ├── OrderRepository.cs
│   │   ├── CartRepository.cs
│   │   ├── ReviewRepository.cs
│   │   └── BaseRepository.cs    # Generic repository base (if needed)
│   │
│   └── UnitOfWork/
│       └── UnitOfWork.cs        # Implements IUnitOfWork
│
├── Services/                     # External services
│   └── (e.g., EmailService, PaymentService, etc.)
│
├── Migrations/                   # EF Core migrations
│   ├── 202409220000_InitialCreate.cs
│   ├── 202409220100_AddCategoryConstraints.cs
│   └── ...
│
├── DependencyInjection.cs       # Service registration extension
└── Market.Infrastructure.csproj # Dependencies: Application, Domain, EF Core, SQL Server

```

### 4. API Layer (`src/Market.API`)

**Responsibility**: HTTP endpoints, middleware, orchestration. Thin presentation layer.

```
Market.API/
├── Controllers/                  # HTTP endpoints
│   ├── ProductsController.cs
│   ├── CategoriesController.cs
│   ├── UsersController.cs
│   ├── VendorsController.cs
│   ├── OrdersController.cs
│   ├── CartsController.cs
│   └── ReviewsController.cs
│
├── Middleware/                   # HTTP middleware
│   ├── ExceptionHandlingMiddleware.cs
│   ├── LoggingMiddleware.cs
│   └── CorsMiddleware.cs
│
├── Filters/                      # Action filters
│   └── ValidationFilter.cs
│
├── appsettings.json             # Configuration (sensitive values via user-secrets)
├── appsettings.Development.json
├── appsettings.Production.json
│
├── Program.cs                    # Composition root and startup
│
└── Market.API.csproj            # Dependencies: Application, Infrastructure, Domain

```

## Test Projects Structure

```
tests/
├── Market.Domain.Tests/
│   ├── Entities/                # Entity and value object tests
│   ├── Enums/
│   └── Market.Domain.Tests.csproj
│
├── Market.Application.Tests/
│   ├── Features/
│   │   ├── Products/
│   │   ├── Categories/
│   │   └── ...
│   ├── Validators/
│   ├── GlobalUsings.cs
│   └── Market.Application.Tests.csproj
│
├── Market.Infrastructure.Tests/
│   ├── Data/
│   ├── Persistence/
│   └── Market.Infrastructure.Tests.csproj
│
├── Market.API.Tests/
│   ├── Controllers/
│   └── Market.API.Tests.csproj
│
└── Market.Tests.Common/
    ├── Fixtures/               # Test data and mocks
    ├── Builders/               # Test object builders
    └── Market.Tests.Common.csproj

```

## Documentation Structure

```
docs/
├── README.md                    # (via root README.md)
├── API.md                       # REST API endpoint reference
├── ARCHITECTURE.md              # 4-layer design and patterns
├── IMPLEMENTATION.md            # Hands-on code guide
├── CONTRIBUTING.md              # Development guidelines
├── DOCKER.md                    # Container setup
├── TESTING.md                   # Testing guide
├── PROJECT-STRUCTURE.md         # This file
├── CHANGELOG.md                 # Version history
├── DOCUMENTATION.md             # Documentation overview
├── LOCALDB-SETUP.md            # Local database setup
├── workflows-Functional/
│   └── OrderWorkflow.md         # Order processing flow
└── fixs/                        # Historical fix reports
    ├── first-fix.md
    ├── issue-5-cart-price-vulnerability.md
    └── PR.md

```

## Entity Relationships

```
User (1) ──────────── (Many) Vendor
         ├─ User.Id
         └─ Vendor.UserId

Category (1) ────────── (Many) Product
            ├─ Category.Id (PK)
            └─ Product.CategoryId (FK)

Vendor (1) ──────────── (Many) Product
          ├─ Vendor.Id
          └─ Product.VendorId (FK)

Product (1) ────────── (Many) CartItem
           ├─ Product.Id
           └─ CartItem.ProductId (FK)

User (1) ──────────── (Many) Cart
        ├─ User.Id
        └─ Cart.UserId (FK)

Order (1) ──────────── (Many) OrderItem
         ├─ Order.Id
         └─ OrderItem.OrderId (FK)

Product (1) ────────── (Many) OrderItem
           ├─ Product.Id
           └─ OrderItem.ProductId (FK)

Product (1) ────────── (Many) Review
           ├─ Product.Id
           └─ Review.ProductId (FK)

Vendor (1) ──────────── (Many) Review
          ├─ Vendor.Id
          └─ Review.VendorId (FK)

Review (1) ──────────── (Many) ReviewImage
          ├─ Review.Id
          └─ ReviewImage.ReviewId (FK)
```

## Database Schema

**SQL Server** tables:
- Users, Vendors, Products, Categories, ProductTags, Tags
- Orders, OrderItems, Carts, CartItems
- Reviews, ReviewImages
- Migrations history table (auto-created by EF Core)

**Soft-delete column**: `IsDeleted` (bit, default 0) on most tables
**Audit columns**: `CreatedAt` (datetime2), `UpdatedAt` (datetime2, nullable)
**Concurrency**: `RowVersion` (rowversion) on Product table for inventory updates

## Build Artifacts

```
bin/                              # Compiled outputs
├── Debug/
│   └── net9.0/                  # .NET 9 binaries
│       ├── *.dll
│       └── *.exe
└── Release/
    └── net9.0/

obj/                              # Intermediate files (auto-generated)
```

## Key Points

1. **Strict layering** - Each layer depends on layers below only
2. **Domain is independent** - No external dependencies
3. **Application is orchestration** - Commands/Queries delegate to repositories
4. **Infrastructure is implementation** - DbContext, repositories, EF Core
5. **API is thin** - Just route handling, delegates to MediatR
6. **EF Core DbContext** - Centralized in Infrastructure layer
7. **Tests are parallel** - One test project per source layer
8. **Migrations are tracked** - In Infrastructure/Migrations folder
9. **Configuration is externalized** - Via user-secrets and environment variables
10. **No circular dependencies** - Always depends on abstractions, not implementations
