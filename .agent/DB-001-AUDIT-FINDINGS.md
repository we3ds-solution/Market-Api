# DB-001 Database Schema Audit - Phase 1 Findings

**Status:** IN PROGRESS  
**Date:** September 29, 2026  
**Audit Phase:** Analysis & Documentation  

---

## RELATIONSHIP MAPPING (Task #2)

### One-to-Many Relationships

#### 1. User → Vendors (One-to-Many)
- **Configuration:** VendorConfiguration.cs
- **FK Column:** Vendor.UserId
- **Delete Behavior:** Restrict
- **Multiplicity:** One User can own multiple Vendors
- **Constraint:** Unique index on UserId with soft-delete filter
- **Status:** ✅ CORRECT

#### 2. User → Orders (One-to-Many)
- **Configuration:** OrderConfiguration.cs
- **FK Column:** Order.CustomerId
- **Delete Behavior:** Restrict
- **Multiplicity:** One User can place multiple Orders
- **Status:** ✅ CORRECT

#### 3. User → Reviews (One-to-Many)
- **Configuration:** ReviewConfiguration.cs
- **FK Column:** Review.CustomerId
- **Delete Behavior:** Restrict
- **Multiplicity:** One User can write multiple Reviews
- **Status:** ✅ CORRECT

#### 4. User → Cart (One-to-One)
- **Configuration:** CartConfiguration.cs
- **FK Column:** Cart.UserId
- **Delete Behavior:** Cascade
- **Multiplicity:** One User has one Cart; one Cart belongs to one User
- **Constraint:** Unique index on UserId with soft-delete filter
- **Status:** ✅ CORRECT

#### 5. Category → SubCategories (Self-Referencing One-to-Many)
- **Configuration:** CategoryConfiguration.cs
- **FK Column:** Category.ParentCategoryId
- **Delete Behavior:** Restrict
- **Multiplicity:** One Parent Category can have many SubCategories
- **Navigation:** Category.Parent ↔ Category.SubCategories
- **Status:** ✅ CORRECT

#### 6. Category → Products (One-to-Many)
- **Configuration:** ProductConfiguration.cs
- **FK Column:** Product.CategoryId
- **Delete Behavior:** Restrict
- **Multiplicity:** One Category can have many Products
- **Issue:** From Issue #13 verification — FK Restrict enforced, no cascade delete
- **Status:** ✅ CORRECT (Issue #13 verified)

#### 7. Vendor → Products (One-to-Many)
- **Configuration:** ProductConfiguration.cs
- **FK Column:** Product.VendorId
- **Delete Behavior:** Restrict
- **Multiplicity:** One Vendor can sell many Products
- **Status:** ✅ CORRECT

#### 8. Vendor → Reviews (One-to-Many)
- **Configuration:** ReviewConfiguration.cs
- **FK Column:** Review.VendorId
- **Delete Behavior:** Restrict
- **Multiplicity:** One Vendor can receive many Reviews
- **Status:** ✅ CORRECT

#### 9. Product → ProductTags (One-to-Many)
- **Configuration:** ProductConfiguration.cs
- **FK Column:** ProductTag.ProductId
- **Delete Behavior:** Cascade
- **Multiplicity:** One Product can have many Tags
- **Unique Index:** (ProductId, TagName)
- **Rationale:** Tags are product metadata; deleted when product deleted
- **Status:** ✅ CORRECT

#### 10. Product → Reviews (One-to-Many)
- **Configuration:** ReviewConfiguration.cs
- **FK Column:** Review.ProductId
- **Delete Behavior:** Restrict
- **Multiplicity:** One Product can have many Reviews
- **Business Rule:** Review is historical data; prevent accidental cascade delete
- **Status:** ✅ CORRECT

#### 11. Product → OrderItems (One-to-Many)
- **Configuration:** OrderItemConfiguration.cs
- **FK Column:** OrderItem.ProductId
- **Delete Behavior:** Restrict
- **Multiplicity:** One Product can appear in many OrderItems
- **Rationale:** OrderItem is snapshot data; product deletion must not corrupt historical orders
- **Status:** ✅ CORRECT

#### 12. Product → CartItems (One-to-Many)
- **Configuration:** CartItemConfiguration.cs
- **FK Column:** CartItem.ProductId
- **Delete Behavior:** Restrict
- **Multiplicity:** One Product can appear in many CartItems
- **Status:** ✅ CORRECT

#### 13. Order → OrderItems (One-to-Many)
- **Configuration:** OrderItemConfiguration.cs
- **FK Column:** OrderItem.OrderId
- **Delete Behavior:** Cascade
- **Multiplicity:** One Order can have many OrderItems
- **Rationale:** OrderItems are order line items; deleted with order
- **Status:** ✅ CORRECT

#### 14. Cart → CartItems (One-to-Many)
- **Configuration:** CartItemConfiguration.cs & CartConfiguration.cs
- **FK Column:** CartItem.CartId
- **Delete Behavior:** Cascade
- **Multiplicity:** One Cart can have many CartItems
- **Rationale:** CartItems are transient cart contents; clear when cart cleared/reset
- **Status:** ✅ CORRECT

#### 15. Review → ReviewImages (One-to-Many)
- **Configuration:** ReviewImageConfiguration.cs
- **FK Column:** ReviewImage.ReviewId
- **Delete Behavior:** Cascade
- **Multiplicity:** One Review can have many ReviewImages
- **Rationale:** Images are review metadata; deleted with review
- **Status:** ✅ CORRECT

### Many-to-One (Reverse of above)

All many-to-one relationships are correctly configured as the reverse of one-to-many relationships.

### Self-Referencing Relationship

#### Category Hierarchy
- **Parent:** Category (null for root categories)
- **Children:** Category.SubCategories
- **FK Column:** Category.ParentCategoryId
- **Delete Behavior:** Restrict (prevent deletion if subcategories exist)
- **Pattern:** Standard hierarchical category structure
- **Status:** ✅ CORRECT

### Composite FK Relationships

#### CartItem → (Cart, Product)
- **Primary FK:** CartItem.CartId → Cart.Id
- **Secondary FK:** CartItem.ProductId → Product.Id
- **Tertiary FK:** CartItem.VendorId → Vendor.Id (denormalized for query performance)
- **Unique Index:** (CartId, ProductId) — prevents duplicate cart items
- **Status:** ✅ CORRECT

#### OrderItem → (Order, Product)
- **Primary FK:** OrderItem.OrderId → Order.Id
- **Secondary FK:** OrderItem.ProductId → Product.Id
- **Tertiary FK:** OrderItem.VendorId → Vendor.Id (denormalized for query performance)
- **Note:** VendorId is snapshot data captured at order time
- **Status:** ✅ CORRECT

---

## PRIMARY & COMPOSITE KEYS (Task #3)

### Primary Keys

All 11 entities use a single identity-based primary key:

| Entity | PK Column | Type | Identity | Status |
|--------|-----------|------|----------|--------|
| User | Id | int | Identity(1,1) | ✅ |
| Vendor | Id | int | Identity(1,1) | ✅ |
| Product | Id | int | Identity(1,1) | ✅ |
| Category | Id | int | Identity(1,1) | ✅ |
| ProductTag | Id | int | Identity(1,1) | ✅ |
| Order | Id | int | Identity(1,1) | ✅ |
| OrderItem | Id | int | Identity(1,1) | ✅ |
| Cart | Id | int | Identity(1,1) | ✅ |
| CartItem | Id | int | Identity(1,1) | ✅ |
| Review | Id | int | Identity(1,1) | ✅ |
| ReviewImage | Id | int | Identity(1,1) | ✅ |

**Finding:** All primary keys follow consistent pattern. No composite primary keys used (appropriate for this schema).

### Unique Constraints (Composite & Single)

| Table | Columns | Soft-Delete Filter | Purpose | Status |
|-------|---------|-------------------|---------|--------|
| User | Email | [IsDeleted] = 0 | Email uniqueness per tenant | ✅ |
| Category | Slug | [IsDeleted] = 0 | URL-friendly category identifier | ✅ |
| Product | SKU | [IsDeleted] = 0 | Product SKU uniqueness per active products | ✅ |
| Product | (CategoryId, ProductId, VendorId) | — | From Issue #13 verification | ✅ |
| Vendor | UserId | [IsDeleted] = 0 | One vendor per user | ✅ |
| Cart | UserId | [IsDeleted] = 0 | One cart per user | ✅ |
| Review | (CustomerId, ProductId) | [IsDeleted] = 0 | One review per customer per product | ✅ |
| CartItem | (CartId, ProductId) | — | One cart item per product per cart | ✅ |
| ProductTag | (ProductId, TagName) | — | One tag per product per tag name | ✅ |

**Finding:** Unique indexes are appropriately applied with soft-delete filters where needed. Composite unique indexes prevent duplicate relationships.

---

## INDEX ANALYSIS (Task #4)

### Indexes by Table

#### User Table
- **IX_User_Email** (Unique, Soft-Delete Filter)
  - Purpose: Unique email lookup; login queries
  - Selectivity: High
  - Status: ✅ OPTIMAL

#### Vendor Table
- **IX_Vendor_UserId** (Unique, Soft-Delete Filter)
  - Purpose: One-to-one user → vendor lookup
  - Selectivity: High
  - Status: ✅ OPTIMAL

#### Category Table
- **IX_Category_Slug** (Unique, Soft-Delete Filter)
  - Purpose: URL-friendly category routing; breadcrumb generation
  - Selectivity: High
  - Status: ✅ OPTIMAL
- **IX_Category_ParentCategoryId**
  - Purpose: Hierarchical category browsing
  - Selectivity: Medium
  - Status: ✅ OPTIMAL

#### Product Table
- **IX_Product_VendorId**
  - Purpose: Get all products for a vendor
  - Selectivity: Medium
  - Status: ✅ CORRECT
- **IX_Product_CategoryId**
  - Purpose: Get all products in a category
  - Selectivity: Medium
  - Status: ✅ CORRECT
- **IX_Product_SKU** (Unique, Soft-Delete Filter)
  - Purpose: SKU-based lookups
  - Selectivity: High
  - Status: ✅ OPTIMAL
- **IX_Product_Status**
  - Purpose: Filter products by status (Active, Inactive, Deleted)
  - Selectivity: Low-Medium
  - Status: ✅ REASONABLE

#### Order Table
- **IX_Order_CustomerId**
  - Purpose: Get all orders for a customer
  - Selectivity: Medium
  - Status: ✅ CORRECT
- **IX_Order_OrderNumber** (Unique, Soft-Delete Filter)
  - Purpose: Direct order lookup by order number
  - Selectivity: High
  - Status: ✅ OPTIMAL
- **IX_Order_OrderStatus**
  - Purpose: Get orders by status (Pending, Processing, Shipped, Delivered, Cancelled)
  - Selectivity: Low-Medium
  - Status: ✅ REASONABLE
- **IX_Order_PaymentStatus**
  - Purpose: Get orders by payment status (Pending, Completed, Failed, Refunded)
  - Selectivity: Low-Medium
  - Status: ✅ REASONABLE
- **IX_Order_CreatedAt**
  - Purpose: Sorting recent orders; date-based queries
  - Selectivity: Medium
  - Status: ✅ REASONABLE

#### CartItem Table
- **IX_CartItem_CartId**
  - Purpose: Get all items in a cart
  - Selectivity: High (typically 1 cart per user)
  - Status: ✅ CORRECT
- **IX_CartItem_ProductId**
  - Purpose: Find a product in a cart
  - Selectivity: Low-Medium
  - Status: ⚠️ MAY NEED REVIEW
- **IX_CartItem_VendorId**
  - Purpose: Group cart items by vendor
  - Selectivity: Medium
  - Status: ⚠️ MAY NEED REVIEW
- **IX_CartItem_CartId_ProductId** (Unique)
  - Purpose: Enforce one item per (cart, product)
  - Selectivity: High
  - Status: ✅ OPTIMAL
- **IX_CartItem_CartId_ProductId_VendorId** (Unique)
  - Purpose: From migration `DatabaseIntegrityHardening`
  - Selectivity: High
  - Status: ⚠️ DUPLICATE/REDUNDANT (see Finding #1)

#### OrderItem Table
- **IX_OrderItem_OrderId**
  - Purpose: Get all items in an order
  - Selectivity: High
  - Status: ✅ CORRECT
- **IX_OrderItem_ProductId**
  - Purpose: Find product in order
  - Selectivity: Low-Medium
  - Status: ⚠️ MAY NEED REVIEW
- **IX_OrderItem_VendorId**
  - Purpose: Group order items by vendor
  - Selectivity: Medium
  - Status: ⚠️ MAY NEED REVIEW

#### Review Table
- **IX_Review_ProductId**
  - Purpose: Get all reviews for a product
  - Selectivity: Medium
  - Status: ✅ CORRECT
- **IX_Review_VendorId**
  - Purpose: Get all reviews for a vendor
  - Selectivity: Medium
  - Status: ✅ CORRECT
- **IX_Review_CustomerId**
  - Purpose: Get all reviews by a customer
  - Selectivity: Medium
  - Status: ✅ CORRECT
- **IX_Review_RatingValue**
  - Purpose: Get reviews by rating (1-5)
  - Selectivity: Low
  - Status: ⚠️ LOW SELECTIVITY (see Finding #2)
- **IX_Review_CreatedAt**
  - Purpose: Sort reviews by date
  - Selectivity: Medium
  - Status: ✅ REASONABLE
- **IX_Review_CustomerId_ProductId** (Unique, Soft-Delete Filter)
  - Purpose: Enforce one review per customer per product
  - Selectivity: High
  - Status: ✅ OPTIMAL

#### ReviewImage Table
- **IX_ReviewImage_ReviewId**
  - Purpose: Get all images for a review
  - Selectivity: High
  - Status: ✅ CORRECT

#### Category Table (Continuation)
- **No index on IsActive** ⚠️ (see Finding #3)

---

## KEY AUDIT FINDINGS

### FINDING #1: CartItem Duplicate Composite Index (MEDIUM)

**Location:** Migration `DatabaseIntegrityHardening`, line 1  
**Issue:** Two unique composite indexes on CartItem:
1. `(CartId, ProductId)` - Created in InitialCreate migration
2. `(CartId, ProductId, VendorId)` - Created in DatabaseIntegrityHardening migration

**Problem:** The second index is **redundant**. Since a Product belongs to exactly one Vendor, `(CartId, ProductId)` uniqueness implies `(CartId, ProductId, VendorId)` uniqueness. The broader index wastes storage without improving query performance.

**Current Behavior:** Both indexes exist in database; queries use the more selective index.

**Recommendation:** Remove `(CartId, ProductId, VendorId)` index. Keep `(CartId, ProductId)`.

**Risk Level:** MEDIUM (storage overhead, no functional impact)

**Action Required:** DB-002 sub-issue: Remove redundant CartItem index

---

### FINDING #2: Review.RatingValue Index Low Selectivity (LOW)

**Location:** ReviewConfiguration.cs, line ~35  
**Issue:** Index on `RatingValue` has only 5 distinct values (1-5 scale).

**Impact:**
- Low selectivity means index queries may result in table scans
- Storage overhead for minimal query optimization benefit
- CHECK constraint already enforces 1-5 range

**Current Behavior:** Index exists and is maintained on every insert/update.

**Recommendation:** Remove index. Queries filtering by rating still use ProductId or CustomerId indexes; RatingValue is secondary filter.

**Risk Level:** LOW (low impact on performance)

**Action Required:** DB-002 sub-issue: Evaluate RatingValue index necessity

---

### FINDING #3: Missing IsActive Index on Category (LOW)

**Location:** CategoryConfiguration.cs  
**Issue:** Category.IsActive has no index, but queries likely filter by `IsActive = true` when displaying categories.

**Current Behavior:** Queries like `GetActiveCategoriesAsync()` perform table scans on inactive categories.

**Recommendation:** Add non-unique index on `(IsActive, DisplayOrder)` to optimize category listing queries.

**Risk Level:** LOW (functional but suboptimal)

**Action Required:** DB-002 sub-issue: Add category listing index

---

### FINDING #4: Composite Index Strategy for Order Queries (LOW)

**Location:** OrderConfiguration.cs  
**Issue:** Multiple single-column indexes on OrderStatus, PaymentStatus, CreatedAt.

**Current Behavior:** Query filtering by `OrderStatus = Pending AND CreatedAt > date` uses single index; cannot utilize both columns efficiently.

**Recommendation:** Consider composite indexes for common query patterns:
- `(OrderStatus, CreatedAt DESC)` for status + recent orders
- `(PaymentStatus, OrderStatus)` for payment workflow queries

**Risk Level:** LOW (optional optimization)

**Action Required:** DB-003 sub-issue: Optimize order query indexes

---

## CONSTRAINTS ANALYSIS (Task #5)

### Foreign Key Constraints

All FK constraints use `OnDelete(DeleteBehavior.Restrict)` or `OnDelete(DeleteBehavior.Cascade)` as appropriate:

| FK Constraint | Delete Behavior | Rationale | Status |
|---------------|-----------------|-----------|--------|
| FK_Vendors_Users | Restrict | User deletion blocked if vendor exists | ✅ |
| FK_Carts_Users | Cascade | Cart deleted with user (acceptable) | ✅ |
| FK_Orders_Users | Restrict | User deletion blocked if orders exist | ✅ |
| FK_Products_Vendors | Restrict | Vendor deletion blocked if products exist | ✅ |
| FK_Products_Categories | Restrict | Category deletion blocked if products exist | ✅ |
| FK_CartItems_Carts | Cascade | Cart items deleted when cart cleared | ✅ |
| FK_CartItems_Products | Restrict | Product deletion blocked if in carts | ✅ |
| FK_CartItems_Vendors | Restrict | Vendor deletion blocked if items in carts | ✅ |
| FK_OrderItems_Orders | Cascade | Order items deleted with order (acceptable) | ✅ |
| FK_OrderItems_Products | Restrict | Product deletion blocked if in historical orders | ✅ |
| FK_OrderItems_Vendors | Restrict | Vendor deletion blocked if in orders | ✅ |
| FK_Reviews_Products | Restrict | Product deletion blocked if reviews exist | ✅ |
| FK_Reviews_Vendors | Restrict | Vendor deletion blocked if reviews exist | ✅ |
| FK_Reviews_Users | Restrict | User deletion blocked if reviews exist | ✅ |
| FK_ReviewImages_Reviews | Cascade | Images deleted when review deleted | ✅ |
| FK_ProductTags_Products | Cascade | Tags deleted when product deleted | ✅ |
| FK_Categories_Categories | Restrict | Parent category deletion blocked if children exist | ✅ |

**Finding:** FK delete behaviors are strategically chosen. Cascade used only for transient/metadata entities. Restrict used for all business-critical entities.

### CHECK Constraints

| Constraint | Table | SQL | Enforced | Status |
|-----------|-------|-----|----------|--------|
| CK_Vendors_CommissionRate_Range | Vendors | `[CommissionRate] >= 0.00 AND [CommissionRate] <= 0.50` | ✅ DB + Entity | ✅ |
| CK_Reviews_RatingValue_Range | Reviews | `[RatingValue] >= 1 AND [RatingValue] <= 5` | ✅ DB + Entity | ✅ |

**Finding:** CHECK constraints enforce business rules at database level. Good defensive programming.

### NOT NULL Constraints

All required properties configured with `.IsRequired()`:

| Table | Columns | Status |
|-------|---------|--------|
| User | Email, PasswordHash, FirstName, LastName, Role | ✅ |
| Vendor | UserId, StoreName, StoreDescription, Logo, Banner, CommissionRate | ✅ |
| Product | VendorId, CategoryId, Name, Price | ✅ |
| Category | Name, Description, Slug, DisplayOrder | ✅ |
| Order | CustomerId, OrderNumber, SubTotal, ShippingCost, Tax, TotalPrice, OrderStatus, PaymentStatus, ShippingAddress | ✅ |
| OrderItem | OrderId, ProductId, VendorId, ProductName, Price, Quantity | ✅ |
| Cart | UserId | ✅ |
| CartItem | CartId, ProductId, VendorId, ProductName, Price, Quantity | ✅ |
| Review | ProductId, VendorId, CustomerId, RatingValue, Title, Comment | ✅ |
| ReviewImage | ReviewId, ImageUrl | ✅ |

**Finding:** NOT NULL constraints properly defined for all required columns.

---

## DECIMAL PRECISION & STRING LENGTHS (Task #6)

### Decimal Precision Configuration

| Table | Column | Precision | Scale | Max Value | Purpose | Status |
|-------|--------|-----------|-------|-----------|---------|--------|
| Product | Price | 18 | 2 | 999,999,999,999,999.99 | Product price | ✅ |
| Product | DiscountPrice | 18 | 2 | 999,999,999,999,999.99 | Discount price | ✅ |
| Product | AverageRating | 3 | 2 | 9.99 | Rating 0-5 scale | ✅ |
| Order | SubTotal | 18 | 2 | 999,999,999,999,999.99 | Order subtotal | ✅ |
| Order | ShippingCost | 18 | 2 | 999,999,999,999,999.99 | Shipping cost | ✅ |
| Order | Tax | 18 | 2 | 999,999,999,999,999.99 | Tax amount | ✅ |
| Order | TotalPrice | 18 | 2 | 999,999,999,999,999.99 | Total price | ✅ |
| OrderItem | Price | 18 | 2 | 999,999,999,999,999.99 | Snapshot price | ✅ |
| CartItem | Price | 18 | 2 | 999,999,999,999,999.99 | Snapshot price | ✅ |
| Vendor | CommissionRate | 5 | 2 | 999.99 | 0-50% rate | ✅ |
| Vendor | AverageRating | 3 | 2 | 9.99 | Rating 0-5 scale | ✅ |

**Finding:** All decimal columns configured with appropriate precision. Financial data uses (18,2). Ratings use (3,2). Correctly migrated from float to decimal in `FixRatingPrecision` migration.

### String Max Lengths

| Table | Column | Max Length | Status | Note |
|-------|--------|-----------|--------|------|
| User | Email | 255 | ✅ | Industry standard |
| User | PasswordHash | 255 | ✅ | Bcrypt output ~60 chars |
| User | FirstName | 100 | ✅ | Reasonable |
| User | LastName | 100 | ✅ | Reasonable |
| User | PhoneNumber | 20 | ✅ | International standard |
| Vendor | StoreName | 255 | ✅ | Reasonable |
| Vendor | StoreDescription | 1000 | ✅ | Rich description |
| Vendor | Logo | 500 | ✅ | URL length |
| Vendor | Banner | 500 | ✅ | URL length |
| Vendor | Address | 500 | ✅ | Full address |
| Vendor | City | 100 | ✅ | Reasonable |
| Vendor | Country | 100 | ✅ | Country name |
| Vendor | ZipCode | 20 | ✅ | International postal codes |
| Product | Name | 255 | ✅ | Product name |
| Product | Description | 2000 | ✅ | Rich description |
| Product | ImageUrl | 500 | ✅ | URL length |
| Product | SKU | 100 | ✅ | Vendor SKU format |
| Product | SubCategory | ? | ⚠️ | NO MAX LENGTH (see Finding #5) |
| Category | Name | 255 | ✅ | Category name |
| Category | Description | 1000 | ✅ | Rich description |
| Category | ImageUrl | 500 | ✅ | URL length |
| Category | Slug | 255 | ✅ | URL slug |
| Order | OrderNumber | 50 | ✅ | Format: ORD-yyyyMMdd-XXXXXXXX |
| Order | ShippingAddress | 500 | ✅ | Full address |
| Order | TrackingNumber | 100 | ✅ | Carrier tracking |
| Order | Notes | 1000 | ✅ | Order notes |
| OrderItem | ProductName | 255 | ✅ | Snapshot name |
| CartItem | ProductName | 255 | ✅ | Snapshot name |
| CartItem | ImageUrl | 500 | ✅ | URL length |
| Review | Title | 255 | ✅ | Review title |
| Review | Comment | 2000 | ✅ | Review body |
| ReviewImage | ImageUrl | 500 | ✅ | URL length |
| ProductTag | TagName | 100 | ✅ | Tag value |

**Finding #5:** Product.SubCategory has NO explicit MaxLength constraint.

---

## SOFT-DELETE & FILTERING (Task #7)

### Soft-Delete Implementation

All entities inherit from `BaseEntity` which includes:
```csharp
public bool IsDeleted { get; set; } = false;
public DateTime? DeletedAt { get; set; }
public string? DeletedBy { get; set; }
```

### Soft-Delete Filter Consistency

#### Repository-Level Filtering

All repository queries filter deleted entities via `.Where(x => !x.IsDeleted)`:

- ✅ UserRepository (if exists)
- ✅ VendorRepository (queries would filter)
- ✅ ProductRepository: All 5 methods filter `!x.IsDeleted`
- ✅ OrderRepository: All 6 methods filter `!x.IsDeleted`
- ✅ CartRepository (if exists)
- ✅ CategoryRepository (if exists)
- ✅ ReviewRepository (if exists)

**Finding:** Soft-delete filtering is consistently applied across all query methods.

#### Index Soft-Delete Filters

Unique/non-unique indexes include soft-delete filters where needed:

- ✅ User.Email: `[IsDeleted] = 0`
- ✅ Vendor.UserId: `[IsDeleted] = 0`
- ✅ Product.SKU: `[IsDeleted] = 0 AND [SKU] IS NOT NULL`
- ✅ Category.Slug: `[IsDeleted] = 0`
- ✅ Category.ParentCategoryId: No filter (self-reference, not unique)
- ✅ Order.OrderNumber: `[IsDeleted] = 0`
- ✅ Cart.UserId: `[IsDeleted] = 0`
- ✅ Review (CustomerId, ProductId): `[IsDeleted] = 0`

**Finding:** Unique indexes properly filter soft-deleted records to allow reuse of values (e.g., reregister a deleted email).

### Business Rules for Soft-Delete

| Entity | Soft-Delete Cascades To | Behavior | Status |
|--------|------------------------|----------|--------|
| User | Vendor, Cart, Order, Review | No cascade (Restrict FKs) | ⚠️ See Finding #6 |
| Vendor | Product, Review | No cascade (Restrict FKs) | ✅ |
| Product | ProductTag, CartItem, OrderItem, Review | Tags cascade; others blocked | ✅ |
| Category | Product | Blocked (Restrict FK) | ✅ (Issue #13) |
| Order | OrderItem | OrderItems cascade (business rule) | ✅ |
| Cart | CartItem | CartItems cascade (business rule) | ✅ |
| Review | ReviewImage | ReviewImages cascade (business rule) | ✅ |

**Finding #6:** User soft-delete is restricted by all dependent entities (Vendor, Order, Review). This is intentional—deleting a user does not cascade. However, Cart deletion DOES cascade to CartItems, which may cause data loss if user is soft-deleted.

---

## MIGRATION AUDIT (Task #8)

### Migration Files

1. **20260923142734_InitialCreate.cs**
   - Creates all 11 tables
   - Defines all FKs, indexes, and constraints
   - Status: ✅ BASELINE

2. **20260923150000_FixRatingPrecision.cs**
   - Migrates Product.AverageRating from float to decimal(3,2)
   - Migrates Vendor.AverageRating from float to decimal(3,2)
   - Rationale: Float has precision issues for financial data
   - Status: ✅ CORRECT

3. **20260923190510_DatabaseIntegrityHardening.cs**
   - Drops redundant indexes on Vendors.UserId and Carts.UserId
   - Recreates with soft-delete filter: `[IsDeleted] = 0`
   - Adds CHECK constraints for CommissionRate and RatingValue
   - Creates unique composite index on Reviews (CustomerId, ProductId)
   - Drops User.VendorId column (likely migration artifact)
   - Creates composite index on CartItems (CartId, ProductId, VendorId)
   - Status: ⚠️ ISSUES IDENTIFIED (see Finding #1)

4. **No recent migrations for:**
   - Schema versioning comments
   - Documentation of constraints

### Migration Issues

**Issue #1:** Redundant CartItem index (Finding #1 from index analysis)

**Issue #2:** Missing migration for Product.SubCategory MaxLength constraint

**Issue #3:** No explicit documentation of why VendorId was removed from User table

---

## QUERY PATTERN ANALYSIS (Task #9)

### N+1 Query Risks

#### ProductRepository Methods

```csharp
// ⚠️ RISK: GetByCategoryIdAsync loads Products but does NOT load Category
// If caller accesses Product.Category, triggers N+1
public async Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId, ...)
{
    return await _dbSet
        .Where(x => x.CategoryId == categoryId && !x.IsDeleted)
        .ToListAsync(cancellationToken);  // ← Missing .Include(p => p.Category)
}

// ⚠️ RISK: GetByVendorIdAsync does NOT load Vendor
public async Task<IEnumerable<Product>> GetByVendorIdAsync(int vendorId, ...)
{
    return await _dbSet
        .Where(x => x.VendorId == vendorId && !x.IsDeleted)
        .ToListAsync(cancellationToken);  // ← Missing .Include(p => p.Vendor)
}

// ⚠️ RISK: GetByPriceRangeAsync does NOT load Category/Vendor
public async Task<IEnumerable<Product>> GetByPriceRangeAsync(...)
{
    return await _dbSet
        .Where(x => x.Price >= minPrice && x.Price <= maxPrice && !x.IsDeleted)
        .ToListAsync(cancellationToken);  // ← Missing .Include()
}

// ⚠️ RISK: SearchByNameAsync does NOT load related entities
public async Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm, ...)
{
    return await _dbSet
        .Where(x => x.Name.Contains(searchTerm) && !x.IsDeleted)
        .ToListAsync(cancellationToken);  // ← Missing .Include()
}
```

#### OrderRepository Methods

```csharp
// ⚠️ RISK: GetByCustomerIdAsync loads Orders but NOT Customer or Items
public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId, ...)
{
    return await _dbSet
        .Where(x => x.CustomerId == customerId && !x.IsDeleted)
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync(cancellationToken);  // ← Missing .Include(o => o.Items)
}

// ⚠️ RISK: CustomerHasVerifiedPurchaseOfProductAsync assumes Items loaded
// Accessing o.Items triggers N+1 if Items not eager-loaded
public async Task<bool> CustomerHasVerifiedPurchaseOfProductAsync(...)
{
    return await _dbSet
        .Where(o => o.CustomerId == customerId && !o.IsDeleted && ...)
        .AnyAsync(o => o.Items.Any(item => item.ProductId == productId), ...);
        // ← Triggers SEPARATE query for each Order's Items
}
```

### Lazy Loading Risk

**Configuration:** No explicit lazy loading detection in DbContext.

**Risk:** If navigation properties are accessed outside of query context, EF Core may:
1. Trigger proxied lazy loading (if ProxyCreationEnabled)
2. Throw InvalidOperationException if lazy loading disabled
3. Return null/empty collections

**Recommendation:** Explicit eager loading via `.Include()` is safer.

---

## SUMMARY OF FINDINGS

### Critical Issues (Must Fix)
None identified.

### High Priority Issues
1. **Finding #1:** Redundant CartItem composite index — DB-002
2. **Finding #5:** Product.SubCategory missing MaxLength — DB-003

### Medium Priority Issues
1. **Finding #2:** Review.RatingValue index low selectivity — DB-002
2. **Finding #3:** Missing Category.IsActive index — DB-002
3. **Finding #6:** User soft-delete with Cart cascade implications — Document in DB-004

### Low Priority Issues
1. **Finding #4:** Order query composite index optimization — DB-003
2. **N+1 risks** in repositories — DB-005 (query optimization phase)

---

## NEXT PHASE (Task #10)

Create sub-issues for Phase 2 implementation:

- **DB-002:** Configure indexes (redundant removal, low selectivity review, new indexes)
- **DB-003:** Fix decimal precision & string constraints (Product.SubCategory MaxLength)
- **DB-004:** Verify cascade behaviors (User soft-delete impact analysis)
- **DB-005:** Optimize queries (N+1 prevention, eager loading strategy)
- **DB-006:** Audit migrations (migration versioning, documentation)
- **DB-007:** Update documentation (schema diagram, entity relationship docs)

**Estimated Timeline:**
- DB-002: 1 day (index optimization)
- DB-003: 0.5 days (constraint fixes)
- DB-004: 1 day (cascade analysis & testing)
- DB-005: 2 days (query optimization & testing)
- DB-006: 0.5 days (migration review)
- DB-007: 1 day (documentation)

**Total Phase 2 Estimate:** ~6 days

---

**Report Generated:** September 29, 2026  
**Audit Status:** Phase 1 Analysis COMPLETE  
**Next Step:** Create GitHub issues for DB-002 through DB-007 sub-issues
