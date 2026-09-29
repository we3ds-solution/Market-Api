# Issue #13: Category Relationship & Data Integrity - Analysis

**Date:** September 29, 2026  
**Status:** Analysis Complete  

---

## Executive Summary

The Product → Category relationship is **properly implemented** with correct enforcement of referential integrity across create, update, delete, and query operations. The relationship uses:
- **Foreign Key Type:** Required (non-nullable Int32 `CategoryId`)
- **FK Constraint Behavior:** `OnDelete(DeleteBehavior.Restrict)` at both application and database levels
- **Soft-Delete Strategy:** Both Product and Category use soft-delete (IsDeleted flag, DeletedAt timestamp)
- **Category Active State:** Additional `IsActive` boolean restricts assignment to inactive categories
- **Immutable Historical Data:** OrderItems retain snapshot data independent of Product/Category changes

---

## Category/Product Relationship Model

### Entity Definitions

#### Product Entity (`src/Market.Domain/Entities/Product.cs`)
```csharp
public class Product : BaseEntity
{
    public int VendorId { get; set; }
    public int CategoryId { get; set; }  // ← Required FK, non-nullable
    
    // Navigation properties
    public Vendor Vendor { get; set; } = null!;
    public Category Category { get; set; } = null!;  // ← Required reference
}
```

**Key Observations:**
- `CategoryId` is `int` (non-nullable) - relationship is **required**
- `Category` navigation property is required (not nullable)
- Product cannot exist without a valid Category
- Product cannot exist without a valid Vendor

#### Category Entity (`src/Market.Domain/Entities/Category.cs`)
```csharp
public class Category : BaseEntity
{
    public bool IsActive { get; set; } = true;  // ← Additional active state
    public int? ParentCategoryId { get; set; }  // ← Hierarchical support
    
    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
```

**Key Observations:**
- Categories use soft-delete (inherited from BaseEntity: `IsDeleted`, `DeletedAt`)
- Categories have additional `IsActive` boolean for lifecycle management
- Categories inherit from `BaseEntity` with audit fields (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`)
- Supports hierarchical parent/child categories

### EF Core Configuration

#### Product Configuration (`src/Market.Infrastructure/Data/EntityConfigurations/ProductConfiguration.cs`)
```csharp
// Product → Category FK configuration
builder.HasOne(p => p.Category)
    .WithMany(c => c.Products)
    .HasForeignKey(p => p.CategoryId)
    .OnDelete(DeleteBehavior.Restrict);  // ← Restrict deletion if products reference

// Product → Vendor FK configuration  
builder.HasOne(p => p.Vendor)
    .WithMany(v => v.Products)
    .HasForeignKey(p => p.VendorId)
    .OnDelete(DeleteBehavior.Restrict);
```

**Key Observations:**
- **One-to-Many relationship**: One Category has many Products
- **FK Ownership**: Product is the dependent side (owns CategoryId)
- **Delete Behavior:** `OnDelete(DeleteBehavior.Restrict)` prevents cascade deletion
- **Index:** CategoryId is indexed for query performance
- **Database Constraint:** SQL Server enforces FK via CONSTRAINT (not soft-delete filtered)

#### Category Configuration (`src/Market.Infrastructure/Data/EntityConfigurations/CategoryConfiguration.cs`)
```csharp
// Self-referencing hierarchy only
builder.HasOne(c => c.Parent)
    .WithMany(c => c.SubCategories)
    .HasForeignKey(c => c.ParentCategoryId)
    .OnDelete(DeleteBehavior.Restrict);

// NOTE: Product→Category relationship configured in ProductConfiguration (dependent-side)
```

**Key Observations:**
- Product→Category FK is NOT configured here (comment explicitly notes this)
- Configuring from both sides would create duplicate shadow FK column
- Parent→SubCategory FK also uses Restrict behavior

---

## Create Product Behavior

### CreateProductCommandHandler (`src/Market.Application/Features/Products/Commands/CreateProductCommand.cs`)

**Validation Flow:**
```
1. Validate Product entity (name, description, price, etc.)
2. Verify Vendor exists AND is approved AND is active
3. Verify Category exists AND is active
4. Verify SKU is unique (if provided)
5. Create product with supplied CategoryId
6. Persist to database
```

**Category Validation:**
```csharp
// Line 48-51: Category existence check
var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId, cancellationToken);
if (category == null)
    throw new KeyNotFoundException($"Category with ID {product.CategoryId} not found");

// Line 53-54: Category active state check
if (!category.IsActive)
    throw new InvalidOperationException($"Category with ID {product.CategoryId} is inactive. Only active categories can be assigned to products.");
```

**Key Behaviors:**
- ✅ Rejects non-existent CategoryId with `KeyNotFoundException`
- ✅ Rejects inactive Category (IsActive=false) with `InvalidOperationException`
- ✅ Does NOT accept client-supplied Category object - validates via repository
- ✅ Persists only the CategoryId, not a navigation object
- ✅ Vendor approval/active checks happen before Category checks (early exit)

**Validation Order:**
1. Product field validation (FluentValidation)
2. Vendor existence (KeyNotFoundException if null)
3. Vendor approval state (InvalidOperationException if not approved)
4. Vendor active state (InvalidOperationException if inactive)
5. **Category existence (KeyNotFoundException if null)**
6. **Category active state (InvalidOperationException if inactive)**
7. SKU uniqueness (InvalidOperationException if duplicate)

---

## Update Product Behavior

### UpdateProductInternalCommandHandler (`src/Market.Application/Features/Products/Commands/UpdateProductCommand.cs`)

**Key Constraint:**
```csharp
// Line 110: Cannot change vendor
VendorId = product.VendorId,  // ← Existing vendor preserved

// Line 111: Cannot change category
CategoryId = product.CategoryId,  // ← Existing category preserved
```

**Category Validation During Update:**
```csharp
// Line 112-125: Category active state check (but cannot change category)
var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId, cancellationToken);
if (category == null)
    throw new KeyNotFoundException($"Category with ID {product.CategoryId} not found");

if (!category.IsActive)
    throw new InvalidOperationException($"Category with ID {product.CategoryId} is inactive. Cannot update product assigned to an inactive category.");
```

**Key Behaviors:**
- ✅ CategoryId **cannot be changed** in UpdateProductCommand
- ✅ Validates that existing Category still exists (handles deletion between read/write)
- ✅ Validates that existing Category is still active (handles deactivation between read/write)
- ✅ If Category was deleted or deactivated, update is **rejected** (KeyNotFoundException or InvalidOperationException)
- ✅ **No partial updates** - validation failure prevents any modification

**Validation Order:**
1. Product existence check
2. Vendor existence/approval/active checks
3. **Category existence check (fails if deleted)**
4. **Category active state check (fails if deactivated)**
5. SKU uniqueness check
6. Product field validation
7. Update applied

---

## Delete Category Behavior

### DeleteCategoryInternalCommandHandler (`src/Market.Application/Features/Categories/Commands/DeleteCategoryCommand.cs`)

**FK Restrict Enforcement:**
```csharp
// Line 58-65: Check for active products in category
var productsInCategory = await _unitOfWork.Products.GetByCategoryIdAsync(request.Id, cancellationToken);
if (productsInCategory.Any())
{
    throw new InvalidOperationException(
        $"Cannot delete category with ID {request.Id}. {productsInCategory.Count()} product(s) still reference this category. Delete or reassign these products first.");
}

// Line 68-72: Soft-delete the category
category.Delete();  // Sets IsDeleted=true, DeletedAt=UtcNow
```

**Key Behaviors:**
- ✅ **Restrict Enforcement:** Prevents deletion if any non-deleted Products reference the Category
- ✅ **Soft-Delete:** Sets IsDeleted=true and DeletedAt=UtcNow (not physical deletion)
- ✅ **No Cascade:** Products are NOT deleted when Category is deleted
- ✅ **Application-Level Enforcement:** Check happens in handler, plus database FK constraint
- ✅ **Clear Error Message:** Includes count of blocking products

**Soft-Delete Semantics:**
- Category record remains in database with IsDeleted=true
- GetByIdAsync() filters out soft-deleted categories (GetByIdAsync returns null)
- Historical foreign key references remain valid (physical row still exists)
- Products with reference to deleted Category cannot be updated (Category is "not found")

---

## Repository Query Filtering

### CategoryRepository (`src/Market.Infrastructure/Persistence/Repositories/CategoryRepository.cs`)

```csharp
// All queries filter out soft-deleted categories
public async Task<IEnumerable<Category>> GetActiveCategoriesAsync(...)
{
    return await _dbSet
        .Where(x => x.IsActive && !x.IsDeleted)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync(cancellationToken);
}

public async Task<IEnumerable<Category>> GetRootCategoriesAsync(...)
{
    return await _dbSet
        .Where(x => x.ParentCategoryId == null && !x.IsDeleted)
        .OrderBy(x => x.DisplayOrder)
        .ToListAsync(cancellationToken);
}
```

### ProductRepository (`src/Market.Infrastructure/Persistence/Repositories/ProductRepository.cs`)

```csharp
// Get products by category (filters deleted products)
public async Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId, ...)
{
    return await _dbSet
        .Where(x => x.CategoryId == categoryId && !x.IsDeleted)
        .ToListAsync(cancellationToken);
}
```

**Key Observations:**
- ✅ All Category queries automatically filter `!x.IsDeleted`
- ✅ All Product queries automatically filter `!x.IsDeleted`
- ✅ GetByCategoryIdAsync filters both by CategoryId AND IsDeleted=false
- ✅ Delete Category checks for active products only (via GetByCategoryIdAsync)

---

## Base Entity Soft-Delete Implementation

### BaseEntity (`src/Market.Domain/Common/BaseEntity.cs`)

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Key Observations:**
- ✅ Soft-delete is **NOT filtered at database level** - application layer is responsible
- ✅ Delete() method sets IsDeleted=true and DeletedAt=UtcNow
- ✅ Restore() method can reverse soft-delete
- ✅ Both Category and Product inherit this pattern
- ✅ Audit fields (CreatedBy, UpdatedBy) are updated during deletion

---

## Historical Data Integrity

### OrderItem Snapshot (`src/Market.Domain/Entities/OrderItem.cs`)
From Issue #2 & #9: OrderItems capture snapshot data at order time:
- ProductName (snapshot)
- VendorId (snapshot)  
- Price (snapshot - not FK to Product.Price)
- Quantity

**Key Behavior:**
- ✅ OrderItem snapshots are independent of Product/Category changes
- ✅ Category deletion does not affect OrderItems
- ✅ Product deletion does not affect OrderItems
- ✅ Product price changes do not recalculate historical OrderItems
- ✅ Historical Orders remain financially correct

---

## Summary of Relationship Model

| Aspect | Value |
|--------|-------|
| **Relationship Type** | One-to-Many (One Category → Many Products) |
| **Cardinality** | Required (Product.CategoryId is non-nullable int) |
| **FK Column** | Product.CategoryId (int, NOT NULL) |
| **FK Constraint** | OnDelete(DeleteBehavior.Restrict) |
| **Soft-Delete** | Yes (both entities use IsDeleted flag) |
| **Category Active State** | Additional IsActive boolean (business rule) |
| **Deletion Behavior** | Soft-delete Category (IsDeleted=true); FK Restrict prevents if products reference it |
| **Cascade Behavior** | NO - Products are not cascade-deleted |
| **Update Behavior** | CategoryId cannot be changed in UpdateProduct |
| **Query Filtering** | All repository queries filter !x.IsDeleted |
| **Historical Data** | OrderItems preserve snapshot data independent of Category changes |
| **Vendor Ownership** | Product.VendorId (FK to Vendor) cannot change; Category is vendor-agnostic |
| **Database Constraint** | SQL Server enforces FK via CONSTRAINT (independent of soft-delete filtering) |

---

## Validation Rules Discovered

### Create Product
- ✅ CategoryId must exist (not soft-deleted)
- ✅ Category must be active (IsActive=true)
- ✅ CategoryId must be supplied in request
- ✅ Cannot bypass with null CategoryId (field is required)

### Update Product
- ✅ CategoryId cannot be changed (immutable after creation)
- ✅ Existing Category must still exist (handles deletion between read/write)
- ✅ Existing Category must still be active (handles deactivation between read/write)
- ✅ Update is fully rejected if Category validation fails (no partial updates)

### Delete Category
- ✅ Category must exist (not soft-deleted)
- ✅ No products (non-deleted) can reference the category
- ✅ If products exist, exception is thrown with count
- ✅ Deletion uses soft-delete (IsDeleted=true, DeletedAt=UtcNow)
- ✅ Physical FK record remains, so historical data is not corrupted

### Delete Product
- ✅ Product can be soft-deleted regardless of Category state
- ✅ Category integrity is preserved
- ✅ OrderItems with product snapshots are unaffected

---

## EF Core Configuration Verification

### FK Configuration Status
- ✅ Product→Category FK configured in ProductConfiguration only (single source of truth)
- ✅ OnDelete(DeleteBehavior.Restrict) set correctly
- ✅ HasForeignKey(p => p.CategoryId) declared
- ✅ HasOne/WithMany semantics correct
- ✅ No duplicate FK configuration (CategoryConfiguration does not configure Product→Category)
- ✅ Database FK constraint will be created by migration with Restrict behavior

### Index Configuration
- ✅ CategoryId has index (line 54: `builder.HasIndex(p => p.CategoryId)`)
- ✅ Slug has unique index with soft-delete filter (line 28-30 in CategoryConfiguration)
- ✅ SKU has unique index with soft-delete filter (line 55-57 in ProductConfiguration)

---

## Conclusion

The Product ↔ Category relationship is **correctly implemented** with:

1. **Required FK:** CategoryId is non-nullable, enforcing relationship existence
2. **Proper Validation:** Both CreateProduct and UpdateProduct validate Category existence and active state
3. **FK Restrict Enforcement:** DeleteCategory checks for product references; prevents orphans
4. **Soft-Delete Support:** Both entities use soft-delete pattern with proper filtering
5. **Historical Data Integrity:** OrderItems preserve snapshot data independent of Category/Product changes
6. **Query Consistency:** All repository queries filter deleted entities
7. **Active State:** Additional IsActive flag provides business-rule enforcement
8. **No Cascade Deletion:** Products are preserved when Category is deleted
9. **Immutable Categories:** Product CategoryId cannot be changed after creation
10. **Clean Architecture:** Validation logic in Application layer; persistence constraints in Infrastructure

**Status:** Ready for verification testing (Tasks #2-#12)

---

## Next Steps (Tasks #2-#12)

1. ✅ Task #1: Relationship Analysis - COMPLETE
2. [ ] Task #2: Verify CreateProduct validation behavior
3. [ ] Task #3: Verify UpdateProduct validation behavior  
4. [ ] Task #4: Verify DeleteCategory FK Restrict behavior
5. [ ] Task #5: Verify Product soft-delete preserves Category integrity
6. [ ] Task #6: Verify historical OrderItems are unaffected
7. [ ] Task #7: Verify query filtering consistency
8. [ ] Task #8: Verify EF Core FK configuration
9. [ ] Task #9: Verify database FK constraint
10. [ ] Task #10: Add/enhance comprehensive tests
11. [ ] Task #11: Execute full test suite
12. [ ] Task #12: Generate final verification report

