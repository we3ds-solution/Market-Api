# Contributing to Market API

Thank you for considering contributing to Market API! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing](#testing)
- [Documentation](#documentation)

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. Please be respectful and constructive in your interactions.

### Expected Behavior

- Use welcoming and inclusive language
- Be respectful of differing viewpoints
- Accept constructive criticism gracefully
- Focus on what is best for the community
- Show empathy towards other community members

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server (LocalDB, Express, or Docker)
- Git
- Your favorite IDE (Visual Studio, VS Code, Rider)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/Market-Api.git
   cd Market-Api
   ```

3. Add upstream remote:
   ```bash
   git remote add upstream https://github.com/Mostafa-SAID7/Market-Api.git
   ```

### Set Up Development Environment

1. Restore dependencies:
   ```bash
   dotnet restore
   ```

2. Configure database connection string using .NET user secrets (keeps credentials out of tracked files):
   ```bash
   dotnet user-secrets set --project src/Market.API "ConnectionStrings:DefaultConnection" "Server=.;Database=MarketApiDb;Trusted_Connection=true;TrustServerCertificate=true;"
   ```
   
   For Docker environments, set this in your `.env` file instead (keep `.env` untracked).

3. Run the application:
   ```bash
   cd src/Market.API
   dotnet run
   ```

## Development Workflow

### 1. Create a Branch

Always create a new branch for your work:

```bash
git checkout -b feature/your-feature-name
```

Branch naming conventions:
- `feature/` - New features
- `bugfix/` - Bug fixes
- `hotfix/` - Urgent fixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring
- `test/` - Test additions or updates

### 2. Make Changes

- Write clean, readable code
- Follow the coding standards (see below)
- Add tests for new functionality
- Update documentation as needed

### 3. Test Your Changes

```bash
# Run the application
dotnet run

# Run tests (when available)
dotnet test

# Check for build errors
dotnet build
```

### 4. Commit Your Changes

Follow the commit guidelines (see below):

```bash
git add .
git commit -m "feat: add product search functionality"
```

### 5. Push to Your Fork

```bash
git push origin feature/your-feature-name
```

### 6. Create a Pull Request

- Go to the original repository on GitHub
- Click "New Pull Request"
- Select your branch
- Fill out the PR template
- Submit the PR

## Coding Standards

### C# Style Guide

Follow Microsoft's C# coding conventions:

#### Naming Conventions

```csharp
// PascalCase for classes, methods, properties
public class ProductRepository { }
public async Task<Product> GetByIdAsync(string id) { }
public string ProductName { get; set; }

// camelCase for local variables and parameters
var productList = new List<Product>();
public void ProcessProduct(Product product) { }

// _camelCase for private fields
private readonly IMongoCollection<Product> _collection;

// UPPER_CASE for constants
private const int MAX_RETRY_COUNT = 3;
```

#### Code Organization

```csharp
// 1. Using statements
using System;
using System.Collections.Generic;

// 2. Namespace
namespace Market.Application.Features.Products
{
    // 3. Class
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponse>
    {
        // 4. Private fields
        private readonly IUnitOfWork _unitOfWork;
        
        // 5. Constructor
        public CreateProductCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateProductCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
        }
        
        // 6. Public methods
        public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // Validate category exists and is active
            var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null)
                throw new KeyNotFoundException($"Category with ID {request.CategoryId} not found");

            return await CreateProductAsync(request, cancellationToken);
        }
        
        // 7. Private methods
        private async Task<ProductResponse> CreateProductAsync(CreateProductCommand request, CancellationToken cancellationToken)
        {
            // Implementation
        }
    }
}
```

#### Best Practices

1. **Use async/await**:
   ```csharp
   public async Task<Product> GetByIdAsync(string id)
   {
       return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
   }
   ```

2. **Use dependency injection**:
   ```csharp
   public class ProductsController : ControllerBase
   {
       private readonly IProductRepository _repository;
       
       public ProductsController(IProductRepository repository)
       {
           _repository = repository;
       }
   }
   ```

3. **Use meaningful names**:
   ```csharp
   // Good
   var activeProducts = await _repository.GetActiveProductsAsync();
   
   // Bad
   var list = await _repository.GetAsync();
   ```

4. **Keep methods small**:
   - One method should do one thing
   - Aim for methods under 20 lines
   - Extract complex logic into separate methods

5. **Use LINQ appropriately**:
   ```csharp
   var expensiveProducts = products
       .Where(p => p.Price > 100)
       .OrderByDescending(p => p.Price)
       .ToList();
   ```

## Commit Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/):

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Examples

```bash
# Feature
git commit -m "feat(products): add price range filter"

# Bug fix
git commit -m "fix(repository): handle null reference in GetByIdAsync"

# Documentation
git commit -m "docs(readme): update installation instructions"

# Refactoring
git commit -m "refactor(controller): simplify error handling logic"
```

### Detailed Commit

```bash
git commit -m "feat(products): add pagination support

- Add page and pageSize parameters to GetAllAsync
- Implement Skip and Take in repository
- Update controller to accept pagination params
- Add pagination metadata to response

Closes #123"
```

## Pull Request Process

### Before Submitting

- [ ] Code follows the style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] No new warnings generated
- [ ] Tests added/updated
- [ ] All tests pass
- [ ] Branch is up to date with main

### PR Title

Follow the same format as commit messages:

```
feat(products): add pagination support
```

### PR Description Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
How has this been tested?

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] Tests added/updated
- [ ] All tests pass

## Related Issues
Closes #123
```

### Review Process

1. At least one maintainer must approve
2. All CI checks must pass
3. No merge conflicts
4. Address all review comments

## Testing

### Unit Tests

When adding tests (future):

```csharp
[Fact]
public async Task GetByIdAsync_WithValidId_ReturnsProduct()
{
    // Arrange
    var productId = "507f1f77bcf86cd799439011";
    
    // Act
    var result = await _repository.GetByIdAsync(productId);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(productId, result.Id);
}
```

### Integration Tests

Test with real MongoDB (using TestContainers):

```csharp
[Fact]
public async Task CreateAsync_SavesProductToDatabase()
{
    // Arrange
    var product = new Product { Name = "Test", Price = 99.99m };
    
    // Act
    await _repository.CreateAsync(product);
    
    // Assert
    var saved = await _repository.GetByIdAsync(product.Id);
    Assert.NotNull(saved);
}
```

### Manual Testing

Test your changes manually:

1. Start the application
2. Use Postman or curl to test endpoints
3. Verify responses
4. Check MongoDB data

## Documentation

### Code Comments

Add comments for complex logic:

```csharp
// Calculate discount based on quantity tiers
// 1-10: 0%, 11-50: 10%, 51+: 20%
private decimal CalculateDiscount(int quantity, decimal price)
{
    // Implementation
}
```

### XML Documentation

Add XML docs for public APIs:

```csharp
/// <summary>
/// Retrieves a product by its unique identifier.
/// </summary>
/// <param name="id">The MongoDB ObjectId of the product.</param>
/// <returns>The product if found, null otherwise.</returns>
public async Task<Product> GetByIdAsync(string id)
{
    // Implementation
}
```

### Update Documentation

When adding features, update:
- `README.md` - If it affects setup or usage
- `docs/API.md` - For new endpoints
- `docs/ARCHITECTURE.md` - For architectural changes

## Questions?

- Open an issue for bugs or feature requests
- Start a discussion for questions
- Contact maintainers directly for sensitive issues

## Recognition

Contributors will be recognized in:
- GitHub contributors list
- Release notes
- Project documentation

Thank you for contributing to Market API! 🎉
