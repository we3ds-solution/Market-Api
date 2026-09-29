## [3.2.2](https://github.com/Mostafa-SAID7/Market-Api/compare/v3.2.1...v3.2.2) (2026-09-29)


### Bug Fixes

* Use ErrorMessage instead of Message for ValidationFailure in product commands - Fixed CreateProductCommand to use error.ErrorMessage - Fixed UpdateProductCommand to use error.ErrorMessage - FluentValidation ValidationFailure uses ErrorMessage property, not Message - Resolves CS1061 compilation errors in Release build ([76ed12d](https://github.com/Mostafa-SAID7/Market-Api/commit/76ed12d53ff087bb860835b3f5e7f00ee26caf30))

## [3.2.1](https://github.com/Mostafa-SAID7/Market-Api/compare/v3.2.0...v3.2.1) (2026-09-29)


### Bug Fixes

* **Issue#13:** Category Relationship & Data Integrity - Add FK Restrict enforcement and category validation - Enhance DeleteCategoryInternalCommandHandler to verify no Products reference Category before soft-deleting - Add application-level FK Restrict enforcement with meaningful error messages - Enhance GetProductsByCategoryQuery to validate category exists and is active - Prevent returning products for non-existent or inactive categories - Add test case: DeleteCategory_FailsWhenProductsReferenceIt for FK Restrict coverage - Update test helper BuildDeleteCategoryHandler to mock ProductRepository - Confirm all Category/Product immutability constraints enforced - Verify historical OrderItems remain independent of Category changes - No breaking changes, backward compatible enhancements only - Issues [#1](https://github.com/Mostafa-SAID7/Market-Api/issues/1)-[#12](https://github.com/Mostafa-SAID7/Market-Api/issues/12) integrity preserved ([b9c0b28](https://github.com/Mostafa-SAID7/Market-Api/commit/b9c0b282f730046d6bcaeae307873d64e1d7e705)), closes [Issue#13](https://github.com/Issue/issues/13)

# [3.2.0](https://github.com/Mostafa-SAID7/Market-Api/compare/v3.1.1...v3.2.0) (2026-09-29)


### Features

* **categories:** filter active categories in query and add product validation test suites ([fe1a61a](https://github.com/Mostafa-SAID7/Market-Api/commit/fe1a61ab648cd8cd9df40f3c70bc7e85983547bc))

## [3.1.1](https://github.com/Mostafa-SAID7/Market-Api/compare/v3.1.0...v3.1.1) (2026-09-29)


### Bug Fixes

* **test:** simplify ReviewDuplicationTests to eliminate tuple syntax ([0f3b052](https://github.com/Mostafa-SAID7/Market-Api/commit/0f3b052321b37c4d56507acc97a0983b5e608448))

# [3.1.0](https://github.com/Mostafa-SAID7/Market-Api/compare/v3.0.4...v3.1.0) (2026-09-29)


### Bug Fixes

* **category:** enforce product category relationship and active state validation ([16f42fa](https://github.com/Mostafa-SAID7/Market-Api/commit/16f42fa4b00dd9a8dcf2f608fae3f5c0558f4854))
* **orders:** calculate order pricing server-side ([8320bf7](https://github.com/Mostafa-SAID7/Market-Api/commit/8320bf794e3c727eac3a7197688df5764b148a16))
* **test:** add missing using statements for Moq and Xunit ([9c0a2d7](https://github.com/Mostafa-SAID7/Market-Api/commit/9c0a2d748dfeee97b2729f183af716d60953b089))
* **test:** make ReviewDuplicationTests class static to resolve extension method error ([dc7cf3d](https://github.com/Mostafa-SAID7/Market-Api/commit/dc7cf3d3cebfc51217ebb1ff496dbb15246b7f8f))
* **test:** revert ReviewDuplicationTests to public class (not static) ([c38018e](https://github.com/Mostafa-SAID7/Market-Api/commit/c38018e4680508b4b0268f55dadb3d9eb055d32c))


### Features

* add core e-commerce domain entities, application features, API controllers, and unit tests for products, carts, orders, vendors, and reviews ([38cc490](https://github.com/Mostafa-SAID7/Market-Api/commit/38cc490deec16b95c86a8c6438d5be320e8152ae))

## [3.0.4](https://github.com/Mostafa-SAID7/Market-Api/compare/v3.0.3...v3.0.4) (2026-09-24)


### Bug Fixes

* correct User-Vendor relationship from one-to-one to one-to-many ([7c4d531](https://github.com/Mostafa-SAID7/Market-Api/commit/7c4d531a108b481f9baa40a00aba0fffaead9008))

## [3.0.3](https://github.com/Mostafa-SAID7/Market-Api/compare/v3.0.2...v3.0.3) (2026-09-23)


### Bug Fixes

* simplify database model tests to avoid design-time model access issues ([be27b3e](https://github.com/Mostafa-SAID7/Market-Api/commit/be27b3ee705877d89d190a14fffd1d53f6520676))

## [3.0.2](https://github.com/Mostafa-SAID7/Market-Api/compare/v3.0.1...v3.0.2) (2026-09-23)


### Bug Fixes

* add missing xunit using statements and fix ProductStatus enum reference in tests ([8830df9](https://github.com/Mostafa-SAID7/Market-Api/commit/8830df978007f02be7871ad9d592b6235c65975d))

## [3.0.1](https://github.com/Mostafa-SAID7/Market-Api/compare/v3.0.0...v3.0.1) (2026-09-23)


### Bug Fixes

* protect write flows and enforce order pricing ([13d6908](https://github.com/Mostafa-SAID7/Market-Api/commit/13d690829eeffd4509a2d4600b8f276844cfb774))

# [3.0.0](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.5.1...v3.0.0) (2026-09-23)


### Bug Fixes

* **api:** resolve CartsController routing and add missing using ([030ce90](https://github.com/Mostafa-SAID7/Market-Api/commit/030ce903fec74bc0165f2abaa1925b13463a904b))


### Code Refactoring

* **domain:** remove unused User.VendorId relationship ([c9f517a](https://github.com/Mostafa-SAID7/Market-Api/commit/c9f517ab2673df2078d425a19ea395fe7b5712e8))


### Features

* **application:** implement database integrity business logic ([fed9588](https://github.com/Mostafa-SAID7/Market-Api/commit/fed9588853a575211dabfaec1ebd84c031bb5483))
* **infrastructure:** add comprehensive database integrity constraints ([31081ec](https://github.com/Mostafa-SAID7/Market-Api/commit/31081ec50522784da5bd4cbb973e142fc74842a1))
* **migration:** database integrity hardening migration ([cd91b23](https://github.com/Mostafa-SAID7/Market-Api/commit/cd91b233e25a0abbdac741aaca83953647566334))


### BREAKING CHANGES

* **domain:** User.VendorId property removed
Related: Database integrity hardening initiative

## [2.5.1](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.5.0...v2.5.1) (2026-09-23)


### Bug Fixes

* **ci:** skip ineligible auto-assignees ([74d285d](https://github.com/Mostafa-SAID7/Market-Api/commit/74d285da99d90d2f8b8bb170e2bc9475af0acde9))
* **db:** add rating precision migration ([283a573](https://github.com/Mostafa-SAID7/Market-Api/commit/283a573e2c612dc7e1129eba70b913816935e1fe))

# [2.5.0](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.4.0...v2.5.0) (2026-09-23)


### Bug Fixes

* **db:** resolve shadow FK columns and centralize repository persistence ([60c39cf](https://github.com/Mostafa-SAID7/Market-Api/commit/60c39cfa1dd0a57c5ceeead2bbb8de4c48a793f6))


### Features

* **db:** add initial EF Core migration ([2ff414c](https://github.com/Mostafa-SAID7/Market-Api/commit/2ff414cc35e0404794358434aeb1b683c13affde))

# [2.4.0](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.3.3...v2.4.0) (2026-09-22)


### Features

* **db:** complete Phase 1 database audit investigation ([ca86c21](https://github.com/Mostafa-SAID7/Market-Api/commit/ca86c2161634c977932c3323a6f2b7e161415766))

## [2.3.3](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.3.2...v2.3.3) (2026-09-02)


### Bug Fixes

* resolve package downgrade errors in Market.API and Market.Infrastructure ([7b09eb2](https://github.com/Mostafa-SAID7/Market-Api/commit/7b09eb201a1e73cd52dcc8869472734331ac27c3))

## [2.3.2](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.3.1...v2.3.2) (2026-09-02)


### Bug Fixes

* **tests:** add Microsoft.NET.Test.Sdk to all test projects and remove --no-build from test workflow steps ([8af3dc4](https://github.com/Mostafa-SAID7/Market-Api/commit/8af3dc4149d4717b5536bd7d79919d026f0b8bc2))

## [2.3.1](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.3.0...v2.3.1) (2026-09-02)


### Bug Fixes

* **ci/cd:** correct flag to --store-password-in-clear-text in nuget-publish workflow ([a09d3f7](https://github.com/Mostafa-SAID7/Market-Api/commit/a09d3f7274eb800a6628c66d0903c1e5c24a2610))

# [2.3.0](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.2.0...v2.3.0) (2026-09-02)


### Features

* **ci/cd:** add NuGet publishing workflow, package metadata, interactive issue forms, and governance files ([56bd1b1](https://github.com/Mostafa-SAID7/Market-Api/commit/56bd1b1864df68a7b4fa646835b6414fcb3faae0))

# [2.2.0](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.1.0...v2.2.0) (2026-09-02)


### Features

* optimize API performance, enhance static assets caching, and cleanup build artifacts from git tracking ([d322176](https://github.com/Mostafa-SAID7/Market-Api/commit/d322176ae5c9f703b453463393130ff4cf65cf4f))

# [2.1.0](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.0.1...v2.1.0) (2026-09-02)


### Features

* complete wwwroot UI/UX refactoring + fix product query build errors ([50a8e8e](https://github.com/Mostafa-SAID7/Market-Api/commit/50a8e8edd517dab6e9688319b688a7148764a1ed))

## [2.0.1](https://github.com/Mostafa-SAID7/Market-Api/compare/v2.0.0...v2.0.1) (2026-09-01)


### Bug Fixes

* Correct publish path in dotnet.yml workflow to use src/Market.API ([5152760](https://github.com/Mostafa-SAID7/Market-Api/commit/515276006f24c5c626ac6e86db48325c9adb80f7))

# [2.0.0](https://github.com/Mostafa-SAID7/Market-Api/compare/v1.0.0...v2.0.0) (2026-09-01)


### Features

* Complete Clean Architecture refactor with SOLID principles and pure CQRS ([544fcf4](https://github.com/Mostafa-SAID7/Market-Api/commit/544fcf46e126cef701baa5b9fe479fdbcdbb9407))


### BREAKING CHANGES

* Migrated from MongoDB to SQL Server. Update connection strings and database setup.

- Implement strict 4-layer architecture (Domain → Application → Infrastructure → API)
- Apply SOLID principles throughout codebase
- Implement pure CQRS with MediatR
- Migrate to SQL Server with Entity Framework Core 9.0.19
- Reorganize Infrastructure layer (Data/ for DbContext, Persistence/ for repositories)
- Update all namespaces to reflect new architecture
- Fix package dependency versions (DependencyInjection.Abstractions 10.0.0)
- Optimize Docker build to only include source projects
- Add comprehensive CHANGELOG documentation

# 1.0.0 (2026-08-31)


### Bug Fixes

* add security headers to fix CodeQL X-Frame-Options alert ([30872ea](https://github.com/Mostafa-SAID7/Market-Api/commit/30872eaeda958123c127fc0e47259f31a6ad6c8a))
* add X-Frame-Options and security headers to web.config for CodeQL ([f7f7ea0](https://github.com/Mostafa-SAID7/Market-Api/commit/f7f7ea03c8db790f2baaa81dd42e25c2cc6ad289))
* change release-please type to simple ([e194990](https://github.com/Mostafa-SAID7/Market-Api/commit/e194990ffe2e66c6d0c8f4d4cb4ed4a048c35198))
* Correct 404 routing and enhance code block styling ([eb8c0ab](https://github.com/Mostafa-SAID7/Market-Api/commit/eb8c0ab08961267bb393bd54a385a93d49c01402))
* correct middleware order to properly serve static files ([114d93b](https://github.com/Mostafa-SAID7/Market-Api/commit/114d93bc739129c455a999a63109d8624a66e2a0))
* resolve EntityFramework package downgrade error by aligning versions ([8bdf933](https://github.com/Mostafa-SAID7/Market-Api/commit/8bdf933e9934d08a9d2df0c91ee2cd612f17e705))


### Features

* Add favicon and logo, improve UI styling with cleaner borders ([2848b4d](https://github.com/Mostafa-SAID7/Market-Api/commit/2848b4d527c1d73c256e22948d2a5aa86383fd2c))
* Complete UI/UX improvements and 404 routing fix ([35d2fdf](https://github.com/Mostafa-SAID7/Market-Api/commit/35d2fdf6a696f1e0bfd1317d293e079dfccf52c3))
* Improve button contrast and ensure proper routing ([751b923](https://github.com/Mostafa-SAID7/Market-Api/commit/751b9234e7a11518004dd3cec3ecf24db1497de9)), closes [#ffffff](https://github.com/Mostafa-SAID7/Market-Api/issues/ffffff)
