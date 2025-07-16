### .NET / C# TDD Example: Building a Feature from Scratch

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Immutable;

namespace MyApp.Tests;

// Step 1: Write the first failing test
[TestClass]
public class ShoppingCartTests
{
    [TestMethod]
    public void NewCart_IsEmpty()
    {
        // This test fails because ShoppingCart doesn't exist yet
        ShoppingCart cart = new();
        cart.IsEmpty.ShouldBeTrue();
    }
}

// Step 2: Make it pass with minimal code
namespace MyApp.Domain;

public class ShoppingCart
{
    public bool IsEmpty => true;
}

// Step 3: Commit - we have working code!
// git add .
// git commit -m "feat: add empty ShoppingCart"

// Step 4: Write the next failing test
namespace MyApp.Tests;

[TestMethod]
public void AddItem_ToEmptyCart_CartIsNotEmpty()
{
    // Arrange
    ShoppingCart cart = new();
    CartItem item = new()
    {
        ProductId = "SKU123",
        Quantity = 1,
        UnitPrice = 29.99m
    };
    
    // Act
    ShoppingCart updatedCart = cart.AddItem(item);
    
    // Assert
    updatedCart.IsEmpty.ShouldBeFalse();
    updatedCart.ShouldNotBeSameAs(cart); // Verify immutability
}

// Step 5: Make it pass with minimal code
namespace MyApp.Domain;

public class ShoppingCart
{
    private readonly ImmutableList<CartItem> items;
    
    public ShoppingCart() : this(ImmutableList<CartItem>.Empty) { }
    
    private ShoppingCart(ImmutableList<CartItem> items)
    {
        this.items = items;
    }
    
    public bool IsEmpty => this.items.Count == 0;
    
    public ShoppingCart AddItem(CartItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new(this.items.Add(item));
    }
}

public record CartItem
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

// Step 6: Commit again - tests are green!
// git add .
// git commit -m "feat: add ability to add items to cart"

// Step 7: Refactor assessment - Could we improve the code?
// - The code is clean and simple
// - No duplication
// - Clear intent
// Decision: No refactoring needed yet, move to next test

// Step 8: Continue with more tests...
[TestMethod]
public void AddItem_WithInvalidQuantity_ThrowsException()
{
    // Arrange
    ShoppingCart cart = new();
    CartItem invalidItem = new()
    {
        ProductId = "SKU123",
        Quantity = 0, // Invalid!
        UnitPrice = 29.99m
    };
    
    // Act & Assert
    Should.Throw<ArgumentException>(() => cart.AddItem(invalidItem)).Message.ShouldBe("Quantity must be greater than zero");
}

// Step 9: Make the test pass
public class ShoppingCart
{
    private readonly ImmutableList<CartItem> items;
    
    public ShoppingCart() : this(ImmutableList<CartItem>.Empty) { }
    
    private ShoppingCart(ImmutableList<CartItem> items)
    {
        this.items = items;
    }
    
    public bool IsEmpty => this.items.Count == 0;
    
    public ShoppingCart AddItem(CartItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        
        if (item.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero");
        }

        return new(this.items.Add(item));
    }
}

// Continue this cycle: Red -> Green -> Refactor (if needed) -> Commit
```

### Anti-Patterns in TDD: Mocks, Stubs, and Overuse

#### 3. Overuse of Mocks

```csharp
namespace MyApp.Tests.AntiPatterns;

// ❌ BAD: Mocking everything and testing implementation
[TestClass]
public class OverMockedTests
{
    [TestMethod]
    public void Calculate_CallsAllDependencies()
    {
        // Too many mocks!
        ILogger<TaxService> logger = Substitute.For<ILogger<TaxService>>();
        IValidator validator = Substitute.For<IValidator>();
        ICalculator calculator = Substitute.For<ICalculator>();
        IRepository repository = Substitute.For<IRepository>();
        ICache cache = Substitute.For<ICache>();
        
        TaxService service = new(logger, validator, calculator, repository, cache);
        
        TaxData data = new() { Amount = 100, TaxRate = 0.1m };
        calculator.Calculate(Arg.Any<decimal>(), Arg.Any<decimal>()).Returns(110m);
        validator.Validate(Arg.Any<TaxData>()).Returns(true);
        
        service.Process(data);
        
        // Testing implementation details, not behavior!
        logger.Received(1).LogInformation(Arg.Any<string>(), Arg.Any<object[]>());
        validator.Received(1).Validate(Arg.Any<TaxData>());
        calculator.Received(1).Calculate(100m, 0.1m);
        cache.Received(1).Set(Arg.Any<string>(), Arg.Any<decimal>());
    }
}

// ✅ GOOD: Testing actual behavior with minimal mocking
namespace MyApp.Tests;

[TestClass]
public class TaxServiceTests
{
    [TestMethod]
    public void CalculateTax_WithValidAmount_ReturnsCorrectTax()
    {
        // Using the real implementation for pure business logic
        TaxService service = new();
        
        decimal result = service.CalculateTax(100m, 0.1m);
        
        result.ShouldBe(10m); // 100 * 0.1
    }
    
    [TestMethod]
    public void CalculateTotalWithTax_AppliesCorrectTaxAmount()
    {
        TaxService service = new();
        
        TaxCalculation result = service.CalculateTotalWithTax(100m, 0.08m);
        
        result.OriginalAmount.ShouldBe(100m);
        result.TaxRate.ShouldBe(0.08m);
        result.TaxAmount.ShouldBe(8m);
        result.TotalAmount.ShouldBe(108m);
    }
    
    [TestMethod]
    public async Task ProcessOrder_WithExternalDependency_OnlyMocksWhatIsNecessary()
    {
        // Only mock external dependencies
        IPaymentGateway paymentGateway = Substitute.For<IPaymentGateway>();
        OrderProcessor processor = new(paymentGateway);
        
        Order order = new()
        {
            Id = Guid.NewGuid(),
            Amount = 100m,
            CustomerId = "CUST123"
        };
        
        paymentGateway.ChargeAsync(order.Amount, order.CustomerId)
            .Returns(new PaymentResult { Success = true, TransactionId = "TXN123" });
        
        // Test the actual behavior
        ProcessedOrder result = await processor.ProcessAsync(order);
        
        // Assert on outcomes, not implementation
        result.Status.ShouldBe(OrderStatus.Paid);
        result.TransactionId.ShouldBe("TXN123");
        result.ProcessedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow);
    }
}

// Good service design with minimal dependencies
public class TaxService
{
    public decimal CalculateTax(decimal amount, decimal taxRate)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
            
        if (taxRate < 0 || taxRate > 1)
            throw new ArgumentException("Tax rate must be between 0 and 1", nameof(taxRate));
            
        return Math.Round(amount * taxRate, 2);
    }
    
    public TaxCalculation CalculateTotalWithTax(decimal amount, decimal taxRate)
    {
        decimal taxAmount = CalculateTax(amount, taxRate);
        
        return new TaxCalculation
        {
            OriginalAmount = amount,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            TotalAmount = amount + taxAmount
        };
    }
}

public record TaxCalculation
{
    public required decimal OriginalAmount { get; init; }
    public required decimal TaxRate { get; init; }
    public required decimal TaxAmount { get; init; }
    public required decimal TotalAmount { get; init; }
}
```