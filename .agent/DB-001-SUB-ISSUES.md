# DB-001 Phase 2 Sub-Issues

Based on Phase 1 audit findings, the following sub-issues are recommended for Phase 2 implementation.

---

## DB-002: Configure Indexes (MEDIUM PRIORITY)

**Scope:** Index optimization and redundancy removal  
**Effort:** 1 day  
**Dependencies:** DB-001 (completed)  

### Acceptance Criteria

- [ ] Redundant CartItem composite index `(CartId, ProductId, VendorId)` identified and documented
- [ ] Analysis confirms `(CartId, ProductId)` index is sufficient
- [ ] Migration created to drop redundant index
- [ ] Review.RatingValue index evaluated for query effectiveness
- [ ] Low-selectivity index decision documented (keep or remove)
- [ ] Category.IsActive index added for category listing queries
- [ ] Composite index `(IsActive, DisplayOrder)` created for optimal category browsing
- [ ] All index changes migrated to database
- [ ] Tests verify queries execute with new/removed indexes
- [ ] Performance baseline established

### Technical Details

#### Issue 1: Redundant CartItem Index
- **Current State:** Two unique composite indexes on CartItem
  1. `(CartId, ProductId)` — InitialCreate
  2. `(CartId, ProductId, VendorId)` — DatabaseIntegrityHardening
- **Analysis:** Product.VendorId is immutable (FK relationship enforces single vendor). Index 2 is redundant.
- **Action:** Create migration to drop `IX_CartItem_CartId_ProductId_VendorId`
- **Impact:** No functional change; storage savings (~5-10% index space)

#### Issue 2: Review.RatingValue Index Selectivity
- **Current State:** Index on RatingValue (1-5 scale)
- **Analysis:** Only 5 distinct values; low selectivity
- **Options:**
  - Option A: Remove index (typical for low-selectivity columns)
  - Option B: Keep but monitor query performance
- **Recommendation:** Remove; queries use ProductId or CustomerId indexes; RatingValue is secondary filter
- **Decision Point:** Requires performance testing

#### Issue 3: Missing Category.IsActive Index
- **Current State:** No index on Category.IsActive
- **Analysis:** Category queries likely filter `IsActive = true` for display
- **Action:** Create composite index `(IsActive, DisplayOrder)` for optimal category listing
- **Expected Benefit:** Faster category browser queries (~30-50% improvement on large datasets)

### Migration Required

```csharp
// Pseudocode for migration
migrationBuilder.DropIndex(
    name: "IX_CartItem_CartId_ProductId_VendorId",
    table: "CartItems");

migrationBuilder.CreateIndex(
    name: "IX_Category_IsActive_DisplayOrder",
    table: "Categories",
    columns: new[] { "IsActive", "DisplayOrder" });
```

### Testing Scope

- [ ] Unit tests for cart operations still work
- [ ] Integration tests for product-in-cart scenarios
- [ ] Category listing queries use new index
- [ ] Query execution plans reviewed
- [ ] Performance tests run on indexed queries

---

## DB-003: Fix Decimal Precision & String Constraints (LOW-MEDIUM PRIORITY)

**Scope:** Add missing column constraints  
**Effort:** 0.5 days  
**Dependencies:** DB-001 (completed)  

### Acceptance Criteria

- [ ] Product.SubCategory missing MaxLength identified
- [ ] Appropriate max length determined (255 for consistency)
- [ ] Migration created to add constraint
- [ ] Existing data validated against new constraint
- [ ] Entity configuration updated with MaxLength(255)
- [ ] All string properties in Product verified for max lengths
- [ ] Migration tested locally
- [ ] Documentation updated with constraint rationale

### Technical Details

#### Issue: Product.SubCategory Missing MaxLength
- **Current State:** `public string? SubCategory { get; set; }` — no max length
- **Impact:** Unbounded string in database; no validation at model level
- **Solution:** Add `HasMaxLength(255)` in ProductConfiguration
- **Migration:** AlterColumn SubCategory to nvarchar(255)

### Migration Required

```csharp
// Pseudocode for migration
migrationBuilder.AlterColumn<string>(
    name: "SubCategory",
    table: "Products",
    type: "nvarchar(255)",
    maxLength: 255,
    nullable: true);
```

### Testing Scope

- [ ] Existing SubCategory values fit within 255 chars
- [ ] Model validation rejects > 255 chars
- [ ] Database migration applies cleanly
- [ ] No data loss

---

## DB-004: Verify Cascade Behaviors (MEDIUM PRIORITY)

**Scope:** Document and validate delete behavior implications  
**Effort:** 1 day  
**Dependencies:** DB-001 (completed)  

### Acceptance Criteria

- [ ] User soft-delete behavior documented
- [ ] Impact of User deletion on Cart analyzed
- [ ] Cart cascade to CartItems validated (acceptable behavior)
- [ ] Order/OrderItem cascade implications verified
- [ ] Vendor deletion behavior tested
- [ ] Category deletion behavior tested (Issue #13 already verified)
- [ ] Product deletion impact on historical orders verified
- [ ] Test suite covers all cascade scenarios
- [ ] Documentation created for cascade business rules

### Technical Details

#### Finding #6: User Soft-Delete with Cart Cascade

**Current Behavior:**
- User FK constraints: Restrict (Vendor, Order, Review)
- Cart FK constraint: Cascade

**Issue:** If User is soft-deleted, their Cart is NOT deleted (Restrict prevents cascade from higher level). However, Cart.Items (CartItems) ARE deleted when Cart is deleted via cascade.

**Business Rule to Clarify:**
- Should User soft-deletion cascade to their Cart?
- Should Cart soft-deletion cascade to CartItems or just mark Cart as deleted?

**Recommendation:**
- Document: User soft-delete is permitted only if Vendor, Order, Review FKs are deleted first
- Alternative: Change Cart FK to Restrict instead of Cascade (more conservative)
- Decision Point: Requires product owner input

#### Order → OrderItem Cascade
- **Current:** OrderItem deleted when Order deleted
- **Rationale:** Order is historical transaction record; items are transient line items
- **Status:** ✅ ACCEPTABLE

#### Cart → CartItem Cascade
- **Current:** CartItem deleted when Cart deleted
- **Rationale:** Cart is transient shopping state; items are ephemeral
- **Status:** ✅ ACCEPTABLE

### Test Scenarios

```gherkin
Scenario: User Deletion Blocked if Vendors Exist
  Given a User with 1 Vendor
  When attempting to soft-delete the User
  Then FK Restrict prevents deletion
  And User remains active

Scenario: User Deletion Blocked if Orders Exist
  Given a User with 1 Order
  When attempting to soft-delete the User
  Then FK Restrict prevents deletion
  And User remains active

Scenario: Cart Cascade to CartItems
  Given a Cart with 3 CartItems
  When Cart is deleted
  Then CartItems are cascade-deleted
  And database has no orphaned CartItems

Scenario: Order Cascade to OrderItems
  Given an Order with 5 OrderItems
  When Order is deleted
  Then OrderItems are cascade-deleted
  And database has no orphaned OrderItems
```

---

## DB-005: Optimize Queries (N+1 Prevention) (LOW-MEDIUM PRIORITY)

**Scope:** Add explicit eager loading to repositories  
**Effort:** 2 days  
**Dependencies:** DB-001 (completed), DB-002 (optional)  

### Acceptance Criteria

- [ ] ProductRepository methods updated with `.Include()` calls
- [ ] OrderRepository methods updated with `.Include()` calls
- [ ] CartRepository (if exists) reviewed for N+1 risks
- [ ] All navigation properties eager-loaded where needed
- [ ] Query execution plans reviewed for multiple queries
- [ ] Performance tests show N+1 elimination
- [ ] AsNoTracking used where appropriate (read-only queries)
- [ ] Tests verify lazy loading does not occur
- [ ] Documentation created: Query loading strategy

### Technical Details

#### N+1 Risks Identified

**ProductRepository.GetByCategoryIdAsync**
```csharp
// ❌ BEFORE: N+1 risk
return await _dbSet
    .Where(x => x.CategoryId == categoryId && !x.IsDeleted)
    .ToListAsync(cancellationToken);

// ✅ AFTER: Eager load Category
return await _dbSet
    .Include(p => p.Category)
    .Include(p => p.Vendor)
    .Where(x => x.CategoryId == categoryId && !x.IsDeleted)
    .ToListAsync(cancellationToken);
```

**ProductRepository.GetByVendorIdAsync**
```csharp
// ❌ BEFORE: N+1 risk
return await _dbSet
    .Where(x => x.VendorId == vendorId && !x.IsDeleted)
    .ToListAsync(cancellationToken);

// ✅ AFTER: Eager load relationships
return await _dbSet
    .Include(p => p.Category)
    .Include(p => p.Vendor)
    .Where(x => x.VendorId == vendorId && !x.IsDeleted)
    .ToListAsync(cancellationToken);
```

**OrderRepository.GetByCustomerIdAsync**
```csharp
// ❌ BEFORE: N+1 risk on o.Items
return await _dbSet
    .Where(x => x.CustomerId == customerId && !x.IsDeleted)
    .OrderByDescending(x => x.CreatedAt)
    .ToListAsync(cancellationToken);

// ✅ AFTER: Eager load OrderItems
return await _dbSet
    .Include(o => o.Items)
    .Where(x => x.CustomerId == customerId && !x.IsDeleted)
    .OrderByDescending(x => x.CreatedAt)
    .ToListAsync(cancellationToken);
```

**OrderRepository.CustomerHasVerifiedPurchaseOfProductAsync**
```csharp
// ❌ BEFORE: Separate query per Order's Items
return await _dbSet
    .Where(o => o.CustomerId == customerId && ...)
    .AnyAsync(o => o.Items.Any(item => item.ProductId == productId), ...);

// ✅ AFTER: Single query with Include
return await _dbSet
    .Include(o => o.Items)
    .Where(o => o.CustomerId == customerId && ...)
    .AnyAsync(o => o.Items.Any(item => item.ProductId == productId), ...);
```

### Optimization Strategy

| Query Type | Strategy |
|-----------|----------|
| List queries (GetAll, GetBy...) | Eager load all common navigation properties |
| Detail queries (GetById) | Eager load all navigation properties + nested |
| Existence checks (Any) | Eager load related entities only if needed |
| Read-only queries | Add `.AsNoTracking()` to reduce memory |
| Write operations | Load only required relationships |

### Testing Scope

- [ ] Unit tests for each repository method
- [ ] Integration tests with real database
- [ ] Query execution plan analysis
- [ ] Performance benchmarks (before/after)
- [ ] No lazy loading occurs in production

---

## DB-006: Audit Migrations (LOW PRIORITY)

**Scope:** Review and document migration history  
**Effort:** 0.5 days  
**Dependencies:** DB-001 (completed)  

### Acceptance Criteria

- [ ] Migration purpose documented for each file
- [ ] Schema versioning comments added to migrations
- [ ] Rollback procedures documented (Down method validation)
- [ ] Breaking changes in migrations identified
- [ ] Data migration steps documented (if any)
- [ ] Migration order validated (dependency chain)
- [ ] README updated with migration documentation
- [ ] Migration naming convention verified

### Technical Details

#### Current Migrations

| File | Purpose | Status |
|------|---------|--------|
| 20260923142734_InitialCreate | Schema initialization | ✅ |
| 20260923150000_FixRatingPrecision | Data type correction | ✅ |
| 20260923190510_DatabaseIntegrityHardening | Constraints & hardening | ✅ |

#### Documentation Template

```csharp
/// <summary>
/// Migration: {{Name}}
/// Date: {{Date}}
/// Purpose: {{Brief description}}
/// 
/// Changes:
/// - {{Change 1}}
/// - {{Change 2}}
/// 
/// Breaking Changes: {{List or "None"}}
/// Rollback Impact: {{Impact analysis}}
/// Data Migration: {{Steps or "None"}}
/// </summary>
public partial class {{Name}} : Migration
{
    // ...
}
```

---

## DB-007: Update Documentation (LOW PRIORITY)

**Scope:** Create/update schema documentation  
**Effort:** 1 day  
**Dependencies:** All DB phases  

### Acceptance Criteria

- [ ] Entity relationship diagram (ERD) created
- [ ] Schema documentation file created (SCHEMA.md)
- [ ] Cascade behavior rules documented
- [ ] Soft-delete patterns documented
- [ ] Query optimization guide created
- [ ] Migration guide created
- [ ] Constraints rationale documented
- [ ] Index strategy documented
- [ ] Architecture ADR created (schema design decisions)

### Technical Details

#### Documentation Deliverables

1. **SCHEMA.md**
   - Overview of all 11 entities
   - Relationship diagram (text or image)
   - Primary keys and indexes
   - Constraints and business rules
   - Soft-delete behavior

2. **QUERY_PATTERNS.md**
   - Recommended query patterns
   - N+1 prevention strategies
   - Include/ThenInclude examples
   - AsNoTracking usage
   - Performance considerations

3. **MIGRATION_GUIDE.md**
   - How to create new migrations
   - Naming conventions
   - Rollback procedures
   - Testing strategy

4. **ARCHITECTURE_DECISIONS.md**
   - Why Restrict FK constraints
   - Why cascade on ProductTags/CartItems/OrderItems
   - Why soft-delete pattern
   - Why rowversion on Product

---

## IMPLEMENTATION SEQUENCE

Recommended order:

1. **DB-002** (Index Optimization) — Improves performance immediately
2. **DB-003** (String Constraints) — Quick win, reduces database surface area
3. **DB-004** (Cascade Validation) — Ensures business rule compliance
4. **DB-005** (Query Optimization) — Eliminates N+1 issues
5. **DB-006** (Migration Audit) — Documentation and governance
6. **DB-007** (Documentation) — Knowledge base for team

---

## ESTIMATED EFFORT

| Sub-Issue | Estimate | Priority |
|-----------|----------|----------|
| DB-002 | 1 day | MEDIUM |
| DB-003 | 0.5 days | LOW-MEDIUM |
| DB-004 | 1 day | MEDIUM |
| DB-005 | 2 days | LOW-MEDIUM |
| DB-006 | 0.5 days | LOW |
| DB-007 | 1 day | LOW |
| **Total** | **~6 days** | — |

---

## PHASE 2 WORKFLOW

Per enterprise workflow in `.agent/workflow.md`:

### For Each Sub-Issue:

1. **Create GitHub Issue** (Team Lead)
   - Title: "DB-0XX: {{Issue Title}}"
   - Description: Copy relevant acceptance criteria
   - Labels: "database", "enhancement", "phase-2"
   - Assign to: Mohammed Hossam

2. **Create Feature Branch** (Engineer)
   ```bash
   git checkout -b feat/db-0xx-short-description
   ```

3. **Implement & Commit** (Engineer)
   - Make code changes
   - Commit with conventional message: `feat(db): add/fix {{description}}`
   - Push: `git push -u origin feat/db-0xx-...`

4. **Create Pull Request** (Engineer)
   - Reference GitHub issue: `Closes #XXX`
   - Include test evidence
   - Link to DB-001 findings

5. **Code Review** (Team Lead)
   - Verify acceptance criteria met
   - Review schema changes
   - Approve or request changes

6. **Merge** (Team Lead)
   - Squash and merge to main
   - Delete feature branch

7. **Post-Merge Verification** (Engineer)
   - Build passes: `dotnet build`
   - Tests pass: `dotnet test`
   - No regressions
   - Close GitHub issue

---

## RISK ASSESSMENT

| Sub-Issue | Risk Level | Mitigation |
|-----------|-----------|-----------|
| DB-002 | LOW | Index removal is non-breaking; easy rollback |
| DB-003 | LOW | String constraint; existing data should fit |
| DB-004 | MEDIUM | Cascade behavior affects delete operations |
| DB-005 | MEDIUM | Query changes affect performance; needs testing |
| DB-006 | LOW | Documentation only |
| DB-007 | LOW | Documentation only |

---

**Report Generated:** September 29, 2026  
**Phase 1 Audit Status:** COMPLETE  
**Phase 2 Ready for Implementation:** YES

Next step: Create GitHub issues for DB-002 through DB-007.
