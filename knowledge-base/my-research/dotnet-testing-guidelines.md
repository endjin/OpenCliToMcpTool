## .NET / C# Testing Principles

### Behavior-Driven Testing

- **No "unit tests"** - this term is not helpful. Tests should verify expected behavior, treating implementation as a black box
- Test through the public API exclusively - internals should be invisible to tests
- No 1:1 mapping between test files and implementation files
- Tests that examine internal implementation details are wasteful and should be avoided
- **Coverage targets**: 100% coverage should be expected at all times, but these tests must ALWAYS be based on business behaviour, not implementation details
- Tests must document expected business behaviour

### Testing Tools

- Microsoft.Testing.Platform for testing frameworks
- Shouldly for assertions
- NSubstitute for API mocking when needed

### Testing Resources

1. **Microsoft.Testing.Platform**
   - [Official Documentation](https://github.com/microsoft/testfx/docs)
   - [Migration Guide from VSTest](https://github.com/microsoft/testfx/docs/migration)

2. **Shouldly**
   - [Documentation](https://docs.shouldly.org/)
   - [Assertion Examples](https://github.com/shouldly/shouldly)

3. **NSubstitute**
   - [Documentation](https://nsubstitute.github.io/)
   - [Getting Started Guide](https://nsubstitute.github.io/help/getting-started/)


#### Schema Usage in Tests

**CRITICAL**: Tests must use real schemas and types from the main project, not redefine their own.

**Why this matters:**

- **Type Safety**: Ensures tests use the same types as production code
- **Consistency**: Changes to schemas automatically propagate to tests
- **Maintainability**: Single source of truth for data structures
- **Prevents Drift**: Tests can't accidentally diverge from real schemas

**Implementation:**

- All domain schemas should be exported from a shared schema package or module
- Test files should import schemas from the shared location
- If a schema isn't exported yet, add it to the exports rather than duplicating it
- Mock data factories should use the real types derived from real schemas

```csharp
// ❌ BAD: Redefining types in tests
// OrderService.Test.cs
namespace MyApp.Tests;

[TestClass]
public class OrderServiceTests
{
    // Don't redefine the Order type in tests!
    private class TestOrder
    {
        public int Id { get; init; }  // Even in bad example, avoid mutable properties
        public decimal Total { get; init; }
    }
    
    [TestMethod]
    public void ProcessOrder_Test()
    {
        TestOrder order = new() { Id = 1, Total = 100 };
        // This creates drift between test and production types
        // DON'T DO THIS!
    }
}

// ✅ GOOD: Using real domain types
// Domain/Models/Order.cs
namespace MyApp.Domain.Models;

public record Order
{
    public required int Id { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Total { get; init; }
    public required OrderStatus Status { get; init; }
    public ImmutableList<OrderItem> Items { get; init; } = ImmutableList<OrderItem>.Empty;
}

public record OrderItem
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

// OrderService.Test.cs
using MyApp.Domain.Models; // Use the real types
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Collections.Immutable;

namespace MyApp.Tests;

[TestClass]
public class OrderServiceTests
{
    private OrderService orderService = null!;
    
    [TestInitialize]
    public void Setup()
    {
        this.orderService = new OrderService();
    }
    
    [TestMethod]
    public void ProcessOrder_WithValidOrder_UpdatesStatus()
    {
        // Use the actual Order type from domain
        Order order = new()
        {
            Id = 1,
            CustomerId = "CUST123",
            Total = 100m,
            Status = OrderStatus.Pending,
            Items = []
        };
        
        Order result = this.orderService.Process(order);
        
        result.Status.ShouldBe(OrderStatus.Processing);
        result.Id.ShouldBe(order.Id);
        result.CustomerId.ShouldBe(order.CustomerId);
    }
}

// Test data builders should also use real types
public class OrderBuilder
{
    private int id = 1;
    private string customerId = "DEFAULT";
    private decimal total = 0m;
    private OrderStatus status = OrderStatus.Pending;
    private ImmutableList<OrderItem> items = [];
    
    public OrderBuilder WithId(int id)
    {
        this.id = id;
        return this;
    }
    
    public OrderBuilder WithCustomerId(string customerId)
    {
        this.customerId = customerId;
        return this;
    }
    
    public OrderBuilder WithTotal(decimal total)
    {
        this.total = total;
        return this;
    }
    
    public OrderBuilder WithStatus(OrderStatus status)
    {
        this.status = status;
        return this;
    }
    
    public OrderBuilder WithItems(params OrderItem[] orderItems)
    {
        this.items = [..orderItems];
        return this;
    }
    
    public Order Build() => new()
    {
        Id = this.id,
        CustomerId = this.customerId,
        Total = this.total,
        Status = this.status,
        Items = this.items
    };
}
```

## Achieving 100% Coverage Through Business Behavior

Example showing how validation code gets 100% coverage without testing it directly:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shouldly;

namespace MyApp.Tests;

// payment-processor.test.cs
[TestClass]
public class PaymentProcessorTests
{
    private PaymentProcessor processor = null!;
    private IPaymentGateway paymentGateway = null!;
    private ITimeProvider timeProvider = null!;
    
    [TestInitialize]
    public void Setup()
    {
        this.paymentGateway = Substitute.For<IPaymentGateway>();
        this.timeProvider = Substitute.For<ITimeProvider>();
        this.timeProvider.UtcNow.Returns(new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        this.processor = new PaymentProcessor(this.paymentGateway, this.timeProvider);
    }
    
    [TestMethod]
    public async Task ProcessPayment_WithValidPayment_ChargesCorrectAmountAndReturnsSuccess()
    {
        // Arrange
        Payment payment = new()
        {
            Amount = 100.00m,
            Currency = "USD",
            CardNumber = "4111111111111111",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            CardholderName = "John Doe"
        };
        
        PaymentResult gatewayResponse = new()
        { 
            Success = true, 
            TransactionId = "txn_123",
            ProcessedAt = this.timeProvider.UtcNow
        };
        
        this.paymentGateway.ChargeAsync(Arg.Any<PaymentRequest>())
            .Returns(gatewayResponse);
        
        // Act
        PaymentResult result = await this.processor.ProcessAsync(payment);
        
        // Assert - Verify the correct behavior
        result.Success.ShouldBeTrue();
        result.TransactionId.ShouldBe("txn_123");
        result.ProcessedAt.ShouldBe(this.timeProvider.UtcNow);
        
        // Verify the gateway was called with correct parameters
        await this.paymentGateway.Received(1).ChargeAsync(
            Arg.Is<PaymentRequest>(req => 
                req.Amount == 100.00m &&
                req.Currency == "USD" &&
                req.CardToken != null));
    }
    
    [TestMethod]
    public async Task ProcessPayment_WithNegativeAmount_DoesNotChargeAndThrowsValidationException()
    {
        // Arrange
        Payment payment = new()
        { 
            Amount = -10.00m,
            Currency = "USD",
            CardNumber = "4111111111111111",
            ExpiryMonth = 12,
            ExpiryYear = 2025
        };
        
        // Act & Assert
        ValidationException exception = await Should.ThrowAsync<ValidationException>(
            async () => await processor.ProcessAsync(payment));
        
        exception.Message.ShouldBe("Payment amount must be greater than zero");
        
        // Verify gateway was never called
        await paymentGateway.DidNotReceive().ChargeAsync(Arg.Any<PaymentRequest>());
    }
    
    [TestMethod]
    public async Task ProcessPayment_WithExpiredCard_DoesNotChargeAndThrowsValidationException()
    {
        // Arrange
        Payment payment = new()
        {
            Amount = 100.00m,
            Currency = "USD",
            CardNumber = "4111111111111111",
            ExpiryMonth = 1,
            ExpiryYear = 2020,
            CardholderName = "John Doe"
        };
        
        // Act & Assert
        ValidationException exception = await Should.ThrowAsync<ValidationException>(
            async () => await processor.ProcessAsync(payment));
        
        exception.Message.ShouldBe("Card expired on 01/2020");
        
        // Verify gateway was never called
        await paymentGateway.DidNotReceive().ChargeAsync(Arg.Any<PaymentRequest>());
    }
    
    [TestMethod]
    public async Task ProcessPayment_WhenGatewayReturnsFailure_ReturnsFailureResult()
    {
        // Arrange
        Payment payment = new()
        {
            Amount = 100.00m,
            Currency = "USD",
            CardNumber = "4111111111111111",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            CardholderName = "John Doe"
        };
        
        PaymentResult failureResponse = new()
        { 
            Success = false, 
            ErrorCode = "INSUFFICIENT_FUNDS",
            ErrorMessage = "Card declined due to insufficient funds"
        };
        
        paymentGateway.ChargeAsync(Arg.Any<PaymentRequest>())
            .Returns(failureResponse);
        
        // Act
        PaymentResult result = await processor.ProcessAsync(payment);
        
        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INSUFFICIENT_FUNDS");
        result.ErrorMessage.ShouldBe("Card declined due to insufficient funds");
    }
}

// payment-processor.cs (implementation driven by tests)
namespace MyApp.Services;

public class PaymentProcessor(IPaymentGateway paymentGateway, ITimeProvider timeProvider)
{
    public async Task<PaymentResult> ProcessAsync(Payment payment)
    {
        // Validation driven by failing tests
        ArgumentNullException.ThrowIfNull(payment);
        
        if (payment.Amount <= 0)
            throw new ValidationException("Payment amount must be greater than zero");
            
        DateOnly expiryDate = new(payment.ExpiryYear, payment.ExpiryMonth, 1);
        expiryDate = expiryDate.AddMonths(1).AddDays(-1);
        DateOnly currentDate = DateOnly.FromDateTime(timeProvider.UtcNow);
        
        if (expiryDate < currentDate)
            throw new ValidationException($"Card expired on {payment.ExpiryMonth:00}/{payment.ExpiryYear}");
        
        PaymentRequest request = new()
        {
            Amount = payment.Amount,
            Currency = payment.Currency,
            CardToken = TokenizeCard(payment),
            Timestamp = timeProvider.UtcNow
        };
        
        return await paymentGateway.ChargeAsync(request);
    }
    
    private static string TokenizeCard(Payment payment)
    {
        ArgumentException.ThrowIfNullOrEmpty(payment.CardNumber);
        
        // Simple tokenization for demonstration
        int lastFourStart = Math.Max(0, payment.CardNumber.Length - 4);
        string lastFour = payment.CardNumber[lastFourStart..];
        return $"tok_{Guid.NewGuid():N}_{lastFour}";
    }
}

// Supporting types
public record Payment
{
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string CardNumber { get; init; }
    public required int ExpiryMonth { get; init; }
    public required int ExpiryYear { get; init; }
    public string? CardholderName { get; init; }
}

public record PaymentRequest
{
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string CardToken { get; init; }
    public required DateTime Timestamp { get; init; }
}

public record PaymentResult
{
    public required bool Success { get; init; }
    public string? TransactionId { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
```
