# SOLID Audit Findings - Market-Api

**Date:** September 29, 2026  
**Scope:** Complete codebase audit for SOLID principle violations  
**Status:** IN PROGRESS  

---

## EXECUTIVE SUMMARY

Market-Api demonstrates strong Clean Architecture with correct dependency flow, minimal abstractions, and cohesive responsibilities. The following **genuine SOLID violations** were identified and will be fixed:

### Critical Findings: 2
### High Findings: 1
### Medium Findings: 2
### Low Findings: 0

---

## DETAILED FINDINGS

### VIOLATION #1: DIP (Dependency Inversion Principle) - CRITICAL

**Location:** 
- `src/Market.Application/Features/Products/Commands/CreateProductCommand.cs:54`
- `src/Market.Application/Features/Products/Commands/UpdateProductCommand.cs:146`

**Problem:**
Handlers directly instantiate validator instances using `new Validators.ProductValidator()`, violating DIP. Application layer creates concrete instances instead of depending on abstractions.

```csharp
// VIOLATION:
var validation = new Validators.ProductValidator().Validate(product);
```

**Why This Is a Genuine Violation:**
- Application layer depends on concrete validator implementation
- Validators are registered in DI but not used
- Prevents validator substitution for testing or alternative implementations
- Duplicates responsibility: handler shouldn't instantiate validators
- Breaks the dependency inversion flow where Application depends on abstractions

**Business Impact:**
- Cannot mock validators in tests without deep intrusion
- Adding conditional validation logic requires modifying handlers
- Validator changes inadvertently affect multiple handlers

**Fix:**
Inject validator abstraction into handlers; or rely on FluentValidation's auto-validation pipeline.

---

### VIOLATION #2: SRP (Single Responsibility Principle) - CRITICAL

**Location:**
`src/Market.API/Middleware/ExceptionHandlingMiddleware.cs:38-60`

**Problem:**
Exception mapping middleware uses a large switch statement to map exceptions to HTTP status codes. The middleware has two responsibilities:
1. Catch unhandled exceptions
2. Transform domain exceptions to HTTP responses

**Code:**
```csharp
switch (exception)
{
    case ArgumentNullException:
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        response.StatusCode = (int)HttpStatusCode.BadRequest;
        response.Message = "Required field is missing.";
        break;
    // ... more cases
}
```

**Why This Is a Genuine Violation:**
- Multiple reasons to change: exception type mapping logic vs middleware pipeline logic
- Adding new exception types requires modifying middleware
- Exception-to-HTTP-status mapping is a separate concern from error handling

**Business Impact:**
- When new exception types are added to domain/application, middleware must change
- Cannot reuse exception-to-status mapping in other contexts
- Difficult to test exception mapping independently

**Fix:**
Extract exception-to-HTTP mapping into a dedicated ExceptionMapper service.

---

### VIOLATION #3: OCP (Open/Closed Principle) - HIGH

**Location:**
`src/Market.Domain/Entities/Order.cs:73-91` and `src/Market.Domain/Entities/Order.cs:105-123`

**Problem:**
Order state transitions use switch expressions for validation, but represent a closed set of domain states. While not immediately problematic, this pattern becomes difficult to extend if custom order types or workflows are needed in the future.

**Current Pattern:**
```csharp
var isValid = (OrderStatus) switch
{
    OrderStatus.Pending => newStatus == OrderStatus.Confirmed || newStatus == OrderStatus.Cancelled,
    OrderStatus.Confirmed => newStatus == OrderStatus.Processing || newStatus == OrderStatus.Cancelled,
    // ... more cases
};
```

**Why This Might Be a Concern:**
- If multi-tenant workflows need different order state machines, this becomes inflexible
- If domain requires role-based state transitions (e.g., admin can transition from Shipped back to Processing), the logic must change
- Represents a closed set; any new order type or workflow requires code change

**Business Impact:**
- Future feature requests for alternate order workflows require code changes to Order entity
- Cannot support multiple order types with different valid state transitions
- Mixing domain rules with technical state machine makes extensibility harder

**Severity:** HIGH (but acceptable for current closed-set domain)

**Fix (Optional):**
This could be refactored to use a State Pattern or Strategy Pattern if domain requirements demand it. Currently, it's acceptable as the order status set is finite and stable.

---

### VIOLATION #4: SRP - MEDIUM

**Location:**
`src/Market.Application/Features/Orders/Commands/UpdateOrderStatusCommand.cs` (and similar handlers)

**Problem:**
Order status update handlers perform validation AND state transition AND persistence. The handler should orchestrate, but responsibility distribution is diffused:

- Validates order exists
- Validates status transition is allowed
- Calls Order.ValidateOrderStatusTransition() (delegation to entity is correct)
- Persists changes

While this is acceptable coordination for a handler, the validation responsibility for "is this a valid order?" bleeds into the handler when it could be in a Domain Service.

**Impact:** MEDIUM - Handlers are somewhat monolithic, but responsibility is focused on orchestration.

**Recommendation:** Create an OrderStateTransitionService in Application layer to encapsulate complex transition rules, or keep as-is if complexity remains manageable.

---

### VIOLATION #5: ISP (Interface Segregation Principle) - MEDIUM

**Location:**
`src/Market.Domain/Repositories/IUnitOfWork.cs`

**Problem:**
IUnitOfWork exposes all 7 repository properties. Clients that only need Product and Category repositories must depend on the full IUnitOfWork interface.

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
    Task SaveAsync(CancellationToken cancellationToken = default);
}
```

**Why This Might Be ISP Violation:**
- A CreateProductCommand only needs IProductRepository, IVendorRepository, ICategoryRepository
- It still depends on all 7 repositories even though it doesn't use them

**Impact:** MEDIUM - In practice, MediatR handles dependency injection automatically, so this is not a practical coupling issue. However, it technically violates ISP principle.

**Current Severity:** MEDIUM (theoretical violation, minimal practical impact)

**Justification for Keeping:**
IUnitOfWork is a logical boundary representing the entire data access layer. Splitting it into multiple interfaces would create unnecessary fragmentation. Current approach is pragmatic.

---

## ARCHITECTURE VERIFICATION

### ✅ Dependency Flow: CORRECT
- API → Application → Domain (correct)
- Infrastructure → Application/Domain (correct)
- No circular dependencies
- No inappropriate cross-layer references

### ✅ Domain Independence: CORRECT
- Domain has NO dependencies on Infrastructure, ASP.NET Core, or Application
- Domain entities are pure (Entity Framework concerns isolated to Infrastructure)

### ✅ Application Layer Purity: MOSTLY CORRECT
- One small DIP violation identified (validator instantiation)
- Otherwise correctly depends only on Domain

### ✅ Thin Controllers: CORRECT
- Controllers use MediatR to dispatch commands/queries
- Controllers handle HTTP concerns only (request/response mapping)
- No business logic in controllers

### ✅ Repository Pattern: CORRECT
- Generic Repository base provides standard operations
- Aggregate-specific repositories add domain queries
- Repositories focus on persistence only
- No business logic in repositories

### ✅ Service Abstractions: CORRECT
- ICurrentUserService properly abstracts user identity
- IProductReadService properly abstracts read optimization
- Services depend on abstractions

---

## ASSESSMENT MATRIX

| Principle | Assessment | Finding | Severity |
|-----------|-----------|---------|----------|
| **S**RP | VIOLATIONS FOUND | Validator instantiation; Exception mapping; Handler orchestration | CRITICAL, MEDIUM |
| **O**CP | ACCEPTABLE | Order state machine (closed, finite); may need review if multi-tenant | HIGH |
| **L**SP | ✅ PASS | No NotSupportedException; all implementations honor contracts | - |
| **I**SP | VIOLATION FOUND | IUnitOfWork exposes all repositories (theoretical, pragmatic trade-off) | MEDIUM |
| **D**IP | VIOLATIONS FOUND | Validator instantiation; Some service dependencies could be better | CRITICAL |

---

## REFACTORING PLAN

### PRIORITY 1: CRITICAL (Fix Immediately)
1. **DIP Violation - Validator Instantiation**
   - Fix: Move validator instantiation to DI container or use ValidationPipeline behavior
   - Files: CreateProductCommand.cs, UpdateProductCommand.cs
   - Impact: Minimal; simplifies testing

2. **SRP Violation - Exception Mapping**
   - Fix: Extract ExceptionMapper service
   - Files: ExceptionHandlingMiddleware.cs, new ExceptionMapper.cs
   - Impact: Minimal; improves extensibility

### PRIORITY 2: HIGH (Fix if Time Permits)
1. **OCP - Order State Machine**
   - Decision: Keep as-is (closed set is appropriate for current domain)
   - Re-evaluate if multi-tenant or role-based workflows needed

### PRIORITY 3: MEDIUM (Document & Monitor)
1. **ISP - IUnitOfWork**
   - Decision: Keep as-is (pragmatic trade-off; fragmentation cost higher than benefit)
   - Consider for future refactoring if coupling becomes problematic

2. **SRP - Handler Orchestration**
   - Decision: Keep current level of coordination
   - Monitor for complexity creep

---

## FILES TO MODIFY

```
src/Market.Application/Features/Products/Commands/CreateProductCommand.cs          (FIX: DIP)
src/Market.Application/Features/Products/Commands/UpdateProductCommand.cs          (FIX: DIP)
src/Market.API/Middleware/ExceptionHandlingMiddleware.cs                           (FIX: SRP)
src/Market.API/Services/ExceptionMapper.cs                                         (NEW: SRP)
src/Market.API/DependencyInjection.cs                                              (UPDATE: Register ExceptionMapper)
```

---

## TESTING IMPACT

- Validator tests: No change (validators are pure logic)
- Handler tests: Mock IValidator instead of ProductValidator
- Middleware tests: Test ExceptionMapper separately from middleware
- Integration tests: No change

---

## NOTES

1. **NO changes to business behavior** - All fixes are refactoring for maintainability
2. **NO changes to public API contracts** - HTTP contracts remain identical
3. **NO changes to database schema** - Persistence layer untouched
4. **Backward compatible** - Existing functionality preserved

