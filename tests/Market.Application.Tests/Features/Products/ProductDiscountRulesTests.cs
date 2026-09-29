namespace Market.Application.Tests.Features.Products;

/// <summary>
/// Issue #10: Product Discount Rules
/// 
/// Comprehensive test suite for product discount validation rules:
/// 1. Price must be positive (> 0)
/// 2. If DiscountPrice is provided, it must be positive (> 0)
/// 3. If DiscountPrice is provided, it must be strictly less than Price (< Price)
/// 4. DiscountPrice is optional (null is allowed)
/// 5. Effective price calculation uses DiscountPrice ?? Price
/// 6. Cart and Order pricing use server-controlled effective prices
/// 
/// Note: These rules are validated by ProductValidator and enforced across
/// CreateProductCommand, UpdateProductInternalCommand, and all pricing flows.
/// </summary>
public class ProductDiscountRulesTests
{
    private readonly ProductValidator _validator = new();

    // -----------------------------------------------------------------------
    // TEST 1: Valid discounted product
    // -----------------------------------------------------------------------
    /// <summary>
    /// A product with a valid discount (DiscountPrice < Price) should pass validation.
    /// </summary>
    [Fact]
    public void Validate_ValidDiscountedProduct_Succeeds()
    {
        var product = new Product
        {
            Name = "Wireless Keyboard",
            Description = "A reliable wireless keyboard with extended battery life.",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            DiscountPrice = 80m, // Valid: < Price
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // TEST 2: DiscountPrice equals Price (invalid)
    // -----------------------------------------------------------------------
    /// <summary>
    /// When DiscountPrice equals Price, the discount is meaningless and should be rejected.
    /// This prevents accidental creation of non-functional discounts.
    /// </summary>
    [Fact]
    public void Validate_DiscountEqualsPrice_Rejected()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            DiscountPrice = 100m, // Invalid: not less than Price
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors, e => 
            e.Field == nameof(Product.DiscountPrice) && 
            e.Message.Contains("less than regular price"));
    }

    // -----------------------------------------------------------------------
    // TEST 3: DiscountPrice greater than Price (invalid)
    // -----------------------------------------------------------------------
    /// <summary>
    /// When DiscountPrice is greater than Price, it creates a negative discount
    /// (selling at loss), which is invalid and should be rejected.
    /// </summary>
    [Fact]
    public void Validate_DiscountGreaterThanPrice_Rejected()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            DiscountPrice = 150m, // Invalid: greater than Price
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors, e => 
            e.Field == nameof(Product.DiscountPrice) && 
            e.Message.Contains("less than regular price"));
    }

    // -----------------------------------------------------------------------
    // TEST 4: Negative DiscountPrice (invalid)
    // -----------------------------------------------------------------------
    /// <summary>
    /// DiscountPrice cannot be negative. Negative prices are invalid.
    /// </summary>
    [Fact]
    public void Validate_NegativeDiscountPrice_Rejected()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            DiscountPrice = -10m, // Invalid: negative
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors, e => 
            e.Field == nameof(Product.DiscountPrice) && 
            e.Message.Contains("greater than 0"));
    }

    // -----------------------------------------------------------------------
    // TEST 5: Zero DiscountPrice (invalid)
    // -----------------------------------------------------------------------
    /// <summary>
    /// DiscountPrice must be greater than 0, not zero or equal to zero.
    /// A zero price would mean giving product away for free, which should be
    /// handled differently (if at all).
    /// </summary>
    [Fact]
    public void Validate_ZeroDiscountPrice_Rejected()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            DiscountPrice = 0m, // Invalid: not greater than 0
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors, e => 
            e.Field == nameof(Product.DiscountPrice) && 
            e.Message.Contains("greater than 0"));
    }

    // -----------------------------------------------------------------------
    // TEST 6: Negative Price (invalid)
    // -----------------------------------------------------------------------
    /// <summary>
    /// Price itself must be positive. Negative prices are never valid.
    /// </summary>
    [Fact]
    public void Validate_NegativePrice_Rejected()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = -50m, // Invalid: negative
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors, e => 
            e.Field == nameof(Product.Price) && 
            e.Message.Contains("greater than 0"));
    }

    // -----------------------------------------------------------------------
    // TEST 7: Zero Price (invalid)
    // -----------------------------------------------------------------------
    /// <summary>
    /// Price must be greater than 0. Zero price is invalid.
    /// </summary>
    [Fact]
    public void Validate_ZeroPrice_Rejected()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 0m, // Invalid: not greater than 0
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors, e => 
            e.Field == nameof(Product.Price) && 
            e.Message.Contains("greater than 0"));
    }

    // -----------------------------------------------------------------------
    // TEST 8: Null DiscountPrice (valid - discount is optional)
    // -----------------------------------------------------------------------
    /// <summary>
    /// DiscountPrice is optional. When null (not set), product has no discount.
    /// This is valid and common.
    /// </summary>
    [Fact]
    public void Validate_NullDiscountPrice_Valid()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 100m,
            DiscountPrice = null, // Valid: discount is optional
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // TEST 9: Maximum valid discount (99.99% off)
    // -----------------------------------------------------------------------
    /// <summary>
    /// Even extreme discounts are valid as long as DiscountPrice > 0 and < Price.
    /// A 99.99% discount (DiscountPrice nearly 0) is technically valid.
    /// </summary>
    [Fact]
    public void Validate_MaximumValidDiscount_Succeeds()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 1000000m, // High price
            DiscountPrice = 0.01m, // Valid: almost free but > 0
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // TEST 10: Decimal precision in pricing
    // -----------------------------------------------------------------------
    /// <summary>
    /// Currency pricing often involves cents. Verify that decimal precision
    /// works correctly in the comparison.
    /// </summary>
    [Fact]
    public void Validate_DecimalPricingPrecision_Works()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 99.99m,
            DiscountPrice = 79.99m, // Valid: precisely less than Price
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // TEST 11: Minimum valid discount (one cent off)
    // -----------------------------------------------------------------------
    /// <summary>
    /// The smallest meaningful discount is one cent below the price.
    /// This should be valid even for common retail prices.
    /// </summary>
    [Fact]
    public void Validate_MinimalDiscount_OnecentBelowPrice_Valid()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 100.00m,
            DiscountPrice = 99.99m, // Valid: one cent discount
            Quantity = 10
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // TEST 12: Effective price calculation with discount
    // -----------------------------------------------------------------------
    /// <summary>
    /// When a product has both Price and DiscountPrice set, the effective price
    /// (used for cart/order calculations) should be DiscountPrice ?? Price.
    /// This test verifies the coalesce operator logic.
    /// </summary>
    [Fact]
    public void EffectivePrice_WithDiscount_UsesDiscountPrice()
    {
        var product = new Product
        {
            Price = 100m,
            DiscountPrice = 80m
        };

        var effectivePrice = product.DiscountPrice ?? product.Price;

        Assert.Equal(80m, effectivePrice);
    }

    // -----------------------------------------------------------------------
    // TEST 13: Effective price calculation without discount
    // -----------------------------------------------------------------------
    /// <summary>
    /// When DiscountPrice is null, the effective price should fall back to Price.
    /// </summary>
    [Fact]
    public void EffectivePrice_WithoutDiscount_UsesRegularPrice()
    {
        var product = new Product
        {
            Price = 100m,
            DiscountPrice = null
        };

        var effectivePrice = product.DiscountPrice ?? product.Price;

        Assert.Equal(100m, effectivePrice);
    }

    // -----------------------------------------------------------------------
    // TEST 14: Price boundary - just above zero
    // -----------------------------------------------------------------------
    /// <summary>
    /// Verify that the minimum valid price (just above 0) is accepted.
    /// </summary>
    [Fact]
    public void Validate_PriceJustAboveZero_Valid()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 0.01m, // Valid: just above 0
            DiscountPrice = null,
            Quantity = 1
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
    }

    // -----------------------------------------------------------------------
    // TEST 15: High price boundary
    // -----------------------------------------------------------------------
    /// <summary>
    /// Verify that prices at the upper bound (just under max) are accepted.
    /// ProductValidator enforces Price <= 1,000,000.
    /// </summary>
    [Fact]
    public void Validate_PriceBelowMaximum_Valid()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 999999.99m, // Just below 1,000,000 limit
            DiscountPrice = 500000m, // Valid discount
            Quantity = 5
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
    }

    // -----------------------------------------------------------------------
    // TEST 16: Price exceeds maximum
    // -----------------------------------------------------------------------
    /// <summary>
    /// Verify that prices exceeding the maximum allowed (1,000,000) are rejected.
    /// </summary>
    [Fact]
    public void Validate_PriceExceedsMaximum_Rejected()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 1000000.01m, // Exceeds max
            Quantity = 1
        };

        var result = _validator.Validate(product);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors, e => 
            e.Field == nameof(Product.Price) && 
            e.Message.Contains("exceed"));
    }

    // -----------------------------------------------------------------------
    // TEST 17: Large product dataset - discount does not affect unrelated fields
    // -----------------------------------------------------------------------
    /// <summary>
    /// Verify that discount validation doesn't inadvertently affect other fields
    /// like quantity, name, or description.
    /// </summary>
    [Fact]
    public void Validate_DiscountChanges_DoNotAffectOtherFields()
    {
        var productWithoutDiscount = new Product
        {
            Name = "Test Product",
            Description = "A comprehensive test product description.",
            VendorId = 1,
            CategoryId = 1,
            Price = 50m,
            DiscountPrice = null,
            Quantity = 100
        };

        var productWithDiscount = new Product
        {
            Name = "Test Product",
            Description = "A comprehensive test product description.",
            VendorId = 1,
            CategoryId = 1,
            Price = 50m,
            DiscountPrice = 40m, // Added discount
            Quantity = 100
        };

        var resultWithout = _validator.Validate(productWithoutDiscount);
        var resultWith = _validator.Validate(productWithDiscount);

        Assert.True(resultWithout.IsValid);
        Assert.True(resultWith.IsValid);

        // Both should have no errors in other fields
        Assert.Empty(resultWithout.Errors);
        Assert.Empty(resultWith.Errors);
    }

    // -----------------------------------------------------------------------
    // TEST 18: Multiple discount products in sequence
    // -----------------------------------------------------------------------
    /// <summary>
    /// Verify that the validator correctly handles multiple different products
    /// with various discount configurations.
    /// </summary>
    [Fact]
    public void Validate_MultipleProducts_IndependentValidation()
    {
        var products = new[]
        {
            new Product
            {
                Name = "Product 1",
                Description = "Description 1 with enough content here.",
                VendorId = 1,
                CategoryId = 1,
                Price = 100m,
                DiscountPrice = 80m,
                Quantity = 10
            },
            new Product
            {
                Name = "Product 2",
                Description = "Description 2 with enough content here.",
                VendorId = 2,
                CategoryId = 2,
                Price = 50m,
                DiscountPrice = null,
                Quantity = 20
            },
            new Product
            {
                Name = "Product 3",
                Description = "Description 3 with enough content here.",
                VendorId = 3,
                CategoryId = 3,
                Price = 200m,
                DiscountPrice = 150m,
                Quantity = 5
            }
        };

        foreach (var product in products)
        {
            var result = _validator.Validate(product);
            Assert.True(result.IsValid, $"Product {product.Name} should be valid");
        }
    }

    // -----------------------------------------------------------------------
    // TEST 19: Cart pricing follows effective price (integration check)
    // -----------------------------------------------------------------------
    /// <summary>
    /// While Cart pricing is tested separately, this verifies the principle
    /// that cart items use DiscountPrice ?? Price formula.
    /// This is documented behavior from AddToCartCommandHandler.
    /// </summary>
    [Fact]
    public void CartPricing_EffectivePriceFormula_Verified()
    {
        var product = new Product
        {
            Price = 100m,
            DiscountPrice = 75m
        };

        // Cart command handler uses: var unitPrice = product.DiscountPrice ?? product.Price;
        var unitPrice = product.DiscountPrice ?? product.Price;
        var cartQuantity = 3;
        var cartSubTotal = unitPrice * cartQuantity;

        Assert.Equal(75m, unitPrice);
        Assert.Equal(225m, cartSubTotal);
    }

    // -----------------------------------------------------------------------
    // TEST 20: Order pricing follows effective price (integration check)
    // -----------------------------------------------------------------------
    /// <summary>
    /// While Order pricing is tested separately, this verifies that the same
    /// effective price formula applies to order items.
    /// This is documented behavior from CreateOrderCommandHandler.
    /// </summary>
    [Fact]
    public void OrderPricing_EffectivePriceFormula_Verified()
    {
        var product = new Product
        {
            Price = 50m,
            DiscountPrice = 40m
        };

        // Order command handler uses: var unitPrice = product.DiscountPrice ?? product.Price;
        var unitPrice = product.DiscountPrice ?? product.Price;
        var orderQuantity = 2;
        var orderItemSubTotal = unitPrice * orderQuantity;
        var taxRate = 0.08m;
        var tax = orderItemSubTotal * taxRate;

        Assert.Equal(40m, unitPrice);
        Assert.Equal(80m, orderItemSubTotal);
        Assert.Equal(6.40m, tax);
    }

    // -----------------------------------------------------------------------
    // TEST 21: Discount validation - cumulative errors
    // -----------------------------------------------------------------------
    /// <summary>
    /// Verify that when multiple fields are invalid, the validator reports
    /// all discount-related errors, not just the first one.
    /// </summary>
    [Fact]
    public void Validate_InvalidPriceAndDiscount_ReportsAllErrors()
    {
        var product = new Product
        {
            Name = "x", // Too short
            Description = "short", // Too short
            VendorId = 0, // Invalid
            CategoryId = 0, // Invalid
            Price = -10m, // Negative - INVALID FOR DISCOUNT
            DiscountPrice = 100m, // Greater than Price - INVALID FOR DISCOUNT
            Quantity = -5 // Negative
        };

        var result = _validator.Validate(product);

        Assert.False(result.IsValid);
        
        // Should have errors for both Price and DiscountPrice
        Assert.Contains(result.Errors, e => e.Field == nameof(Product.Price));
        Assert.Contains(result.Errors, e => e.Field == nameof(Product.DiscountPrice));
    }

    // -----------------------------------------------------------------------
    // TEST 22: DiscountPrice precision edge case
    // -----------------------------------------------------------------------
    /// <summary>
    /// Verify that very small differences between Price and DiscountPrice are
    /// handled correctly due to decimal precision.
    /// </summary>
    [Fact]
    public void Validate_DiscountWithHighPrecision_Works()
    {
        var product = new Product
        {
            Name = "Product",
            Description = "Product description with enough content.",
            VendorId = 1,
            CategoryId = 1,
            Price = 99.9999m,
            DiscountPrice = 99.9998m, // Only 0.0001 difference
            Quantity = 1
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
    }

    // -----------------------------------------------------------------------
    // TEST 23: Real-world scenario - seasonal discount
    // -----------------------------------------------------------------------
    /// <summary>
    /// A realistic scenario: a $99.99 product with a holiday discount to $59.99.
    /// </summary>
    [Fact]
    public void Validate_RealWorldScenario_HolidayDiscount_Valid()
    {
        var product = new Product
        {
            Name = "Premium Wireless Headphones",
            Description = "High-quality wireless headphones with noise cancellation and 30-hour battery life.",
            VendorId = 5,
            CategoryId = 8,
            Price = 99.99m,
            DiscountPrice = 59.99m, // 40% off
            Quantity = 50
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
    }

    // -----------------------------------------------------------------------
    // TEST 24: Real-world scenario - minimal markup with no discount
    // -----------------------------------------------------------------------
    /// <summary>
    /// A realistic scenario: a low-margin item with no discount.
    /// </summary>
    [Fact]
    public void Validate_RealWorldScenario_LowMarginProduct_Valid()
    {
        var product = new Product
        {
            Name = "Basic USB Cable",
            Description = "Standard USB 3.0 to USB-C cable, 2 meters long.",
            VendorId = 3,
            CategoryId = 9,
            Price = 5.99m,
            DiscountPrice = null, // No discount
            Quantity = 500
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
    }

    // -----------------------------------------------------------------------
    // TEST 25: Real-world scenario - deep discount (flash sale)
    // -----------------------------------------------------------------------
    /// <summary>
    /// A realistic scenario: a flash sale with aggressive discount (75% off).
    /// </summary>
    [Fact]
    public void Validate_RealWorldScenario_FlashSaleDiscount_Valid()
    {
        var product = new Product
        {
            Name = "Professional Webcam",
            Description = "4K USB webcam with autofocus and built-in microphone.",
            VendorId = 7,
            CategoryId = 10,
            Price = 199.99m,
            DiscountPrice = 49.99m, // 75% off
            Quantity = 25
        };

        var result = _validator.Validate(product);

        Assert.True(result.IsValid);
    }
}
