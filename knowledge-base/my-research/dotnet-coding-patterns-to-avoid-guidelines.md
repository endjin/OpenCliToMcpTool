## Common C# Patterns to Avoid

### Anti-patterns

#### 1. Testing Implementation Instead of Behavior

```csharp
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shouldly;

namespace MyApp.Tests.AntiPatterns;

// ❌ BAD: Testing private methods or internal state
[TestClass]
public class BadTestExamples
{
    [TestMethod]
    public void ValidateEmail_WithInvalidEmail_ReturnsFalse()
    {
        // Don't use reflection to test private methods!
        UserValidator validator = new();
        MethodInfo? method = validator.GetType().GetMethod("ValidateEmail", BindingFlags.NonPublic | BindingFlags.Instance);
        object? result = method?.Invoke(validator, new[] { "invalid" });
        result.ShouldBe(false); // This is testing HOW not WHAT
    }
    
    // ❌ ALSO BAD: Testing implementation details
    [TestMethod]
    public async Task CreateUser_CallsValidatorExactlyOnce()
    {
        IValidator validator = Substitute.For<IValidator>();
        UserService userService = new(validator);
        
        User user = new() { Email = "test@example.com", Name = "Test" };
        await userService.CreateAsync(user);
        
        // This tests HOW the code works, not WHAT it does
        validator.Received(1).Validate(Arg.Any<User>());
    }
}

// ✅ GOOD: Testing behavior through public API
namespace MyApp.Tests;

[TestClass]
public class UserServiceTests
{
    private IUserRepository repository = null!;
    private IEmailService emailService = null!;
    private UserService userService = null!;
    
    [TestInitialize]
    public void Setup()
    {
        this.repository = Substitute.For<IUserRepository>();
        this.emailService = Substitute.For<IEmailService>();
        this.userService = new UserService(this.repository, this.emailService);
    }
    
    [TestMethod]
    public async Task CreateUser_WithInvalidEmail_DoesNotCreateUserAndThrowsException()
    {
        // Arrange
        User invalidUser = new()
        { 
            Name = "John Doe",
            Email = "invalid-email" // Missing @ symbol
        };
        
        // Act & Assert
        ValidationException exception = await Should.ThrowAsync<ValidationException>(async () => await this.userService.CreateAsync(invalidUser));
        
        exception.Message.ShouldBe("Email must be valid format");
        
        // Verify the behavior - user was not saved
        await this.repository.DidNotReceive().SaveAsync(Arg.Any<User>());
        await this.emailService.DidNotReceive().SendWelcomeEmailAsync(Arg.Any<string>());
    }
    
    [TestMethod]
    public async Task CreateUser_WithValidUser_SavesUserAndSendsWelcomeEmail()
    {
        // Arrange
        User validUser = new()
        {
            Name = "John Doe",
            Email = "john@example.com"
        };
        
        User savedUser = validUser with { Id = Guid.NewGuid() };
        this.repository.SaveAsync(Arg.Any<User>()).Returns(savedUser);
        
        // Act
        User result = await this.userService.CreateAsync(validUser);
        
        // Assert - verify the behavior/outcome
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe(validUser.Name);
        result.Email.ShouldBe(validUser.Email);
        
        // Verify expected side effects occurred
        await this.repository.Received(1).SaveAsync(Arg.Is<User>(u => u.Email == validUser.Email && u.Name == validUser.Name));
        await this.emailService.Received(1).SendWelcomeEmailAsync(validUser.Email);
    }
}
```

#### 2. Mutable State and Side Effects

```csharp
namespace MyApp.AntiPatterns;

// ❌ BAD: Mutating state
public class BadOrderService
{
    private List<Order> orders = new(); // Mutable collection
    
    public void ProcessOrder(Order order)
    {
        // Multiple problems:
        order.Status = "Processing"; // 1. Mutating input parameter
        order.ProcessedAt = DateTime.Now; // 2. Non-deterministic (can't test reliably)
        orders.Add(order); // 3. Hidden side effect in private state
        
        // 4. No return value to indicate success
        // 5. Can't process the same order twice (idempotency issue)
    }
    
    public List<Order> GetOrders() => orders; // Exposes mutable collection!
}

// Bad mutable Order class
public class Order
{
    public int Id { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? ProcessedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

// ✅ GOOD: Immutable operations with explicit dependencies
namespace MyApp.Services;

using System.Collections.Frozen;
using System.Collections.Immutable;

public class OrderService(IOrderRepository repository, ITimeProvider timeProvider, ILogger<OrderService> logger)
{
    // Could use frozen collection for valid statuses
    private static readonly FrozenSet<OrderStatus> ProcessableStatuses = 
        new[] { OrderStatus.Pending, OrderStatus.PaymentReceived }.ToFrozenSet();
    
    public async Task<ProcessedOrder> ProcessOrderAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        
        // Validate input
        if (!ProcessableStatuses.Contains(order.Status))
            throw new InvalidOperationException(
                $"Cannot process order in {order.Status} status. Order must be in one of: {string.Join(", ", ProcessableStatuses)}");
        
        // Create new immutable instance
        ProcessedOrder processedOrder = new()
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Items = order.Items,
            Status = OrderStatus.Processing,
            ProcessedAt = timeProvider.UtcNow,
            OriginalOrderDate = order.OrderDate,
            TotalAmount = CalculateTotal(order.Items)
        };
        
        // Explicit external effects
        await repository.SaveAsync(processedOrder);
        logger.LogInformation(
            "Order {OrderId} for customer {CustomerId} processed at {ProcessedAt} with total {Total:C}", 
            processedOrder.Id, 
            processedOrder.CustomerId,
            processedOrder.ProcessedAt,
            processedOrder.TotalAmount);
        
        // Return the result
        return processedOrder;
    }
    
    private static decimal CalculateTotal(ImmutableList<OrderItem> items) =>
        items.Sum(item => item.Quantity * item.UnitPrice);
}

// Good immutable Order types
public record Order
{
    public required Guid Id { get; init; }
    public required string CustomerId { get; init; }
    public required OrderStatus Status { get; init; }
    public required DateTime OrderDate { get; init; }
    public ImmutableList<OrderItem> Items { get; init; } = [];
}

public record ProcessedOrder
{
    public required Guid Id { get; init; }
    public required string CustomerId { get; init; }
    public required OrderStatus Status { get; init; }
    public required DateTime ProcessedAt { get; init; }
    public required DateTime OriginalOrderDate { get; init; }
    public required decimal TotalAmount { get; init; }
    public ImmutableList<OrderItem> Items { get; init; } = [];
}

public record OrderItem
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}

public enum OrderStatus
{
    Pending,
    PaymentReceived,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
```

### 4. Deep Nesting and Complex Conditionals

```csharp
// ❌ BAD: Deeply nested code with multiple responsibilities
public OrderSummary ProcessOrder(Order order)
{
    if (order != null)
    {
        if (order.Customer != null)
        {
            if (order.Customer.IsActive)
            {
                if (order.Items != null && order.Items.Any())
                {
                    decimal total = 0;
                    decimal discount = 0;
                    
                    foreach (var item in order.Items)
                    {
                        if (item.Product != null)
                        {
                            if (item.Product.IsAvailable)
                            {
                                decimal itemPrice;
                                if (item.Product.IsOnSale)
                                {
                                    itemPrice = item.Product.SalePrice;
                                }
                                else
                                {
                                    itemPrice = item.Product.Price;
                                }
                                
                                decimal lineTotal = itemPrice * item.Quantity;
                                total += lineTotal;
                                
                                // Apply customer discount
                                if (order.Customer.LoyaltyLevel == "Gold")
                                {
                                    discount += lineTotal * 0.1m;
                                }
                                else if (order.Customer.LoyaltyLevel == "Silver")
                                {
                                    discount += lineTotal * 0.05m;
                                }
                            }
                        }
                    }
                    
                    return new()
                    { 
                        Total = total - discount,
                        Discount = discount 
                    };
                }
            }
        }
    }
    return new() { Total = 0, Discount = 0 };
}

// ✅ GOOD: Flat structure with single responsibilities
public OrderSummary ProcessOrder(Order order)
{
    // Early returns for guard clauses
    if (!IsValidOrder(order))
        return OrderSummary.Empty;
    
    // Calculate base total
    var lineItems = CalculateLineItems(order.Items);
    var subtotal = lineItems.Sum(item => item.Total);
    
    // Apply discount based on loyalty
    var discountRate = GetLoyaltyDiscountRate(order.Customer.LoyaltyLevel);
    var discount = subtotal * discountRate;
    
    return new()
    {
        Subtotal = subtotal,
        Discount = discount,
        Total = subtotal - discount,
        LineItems = lineItems
    };
}

private bool IsValidOrder(Order order) =>
    order?.Customer?.IsActive == true && 
    order.Items?.Any() == true;

private ImmutableList<LineItem> CalculateLineItems(IEnumerable<OrderItem> items) =>
    items.Where(item => item.Product?.IsAvailable == true)
         .Select(item => new()
         {
             ProductId = item.Product.Id,
             Quantity = item.Quantity,
             UnitPrice = GetProductPrice(item.Product),
             Total = GetProductPrice(item.Product) * item.Quantity
         })
         .ToImmutableList();

private decimal GetProductPrice(Product product) =>
    product.IsOnSale ? product.SalePrice : product.Price;

private decimal GetLoyaltyDiscountRate(string loyaltyLevel) =>
    loyaltyLevel switch
    {
        "Gold" => 0.10m,
        "Silver" => 0.05m,
        _ => 0m
    };
```

#### 5. Premature Abstraction

```csharp
namespace MyApp.AntiPatterns;

// ❌ BAD: Creating abstractions without clear need
public interface IStringProcessor
{
    string Process(string input);
}

public class UpperCaseProcessor : IStringProcessor
{
    public string Process(string input) => input?.ToUpper() ?? string.Empty;
}

public class LowerCaseProcessor : IStringProcessor
{
    public string Process(string input) => input?.ToLower() ?? string.Empty;
}

public class TrimProcessor : IStringProcessor
{
    public string Process(string input) => input?.Trim() ?? string.Empty;
}

// Over-engineered service
public class StringService
{
    private readonly IStringProcessor processor;
    private readonly ILogger<StringService> logger;
    private readonly IStringProcessorFactory factory;
    
    public StringService(
        IStringProcessor processor, 
        ILogger<StringService> logger,
        IStringProcessorFactory factory)
    {
        this.processor = processor;
        this.logger = logger;
        this.factory = factory;
    }
    
    public string MakeUpperCase(string s)
    {
        this.logger.LogDebug("Processing string: {Input}", s);
        IStringProcessor upperProcessor = this.factory.Create(StringProcessorType.UpperCase);
        string result = upperProcessor.Process(s);
        this.logger.LogDebug("Processed result: {Result}", result);
        return result;
    }
}

// ✅ GOOD: Simple, direct implementation
namespace MyApp.Services;

public class StringService
{
    public string MakeUpperCase(string? input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        return input.ToUpperInvariant();
    }
    
    public string MakeLowerCase(string? input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        return input.ToLowerInvariant();
    }
    
    public string NormalizeWhitespace(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
                return string.Empty;
        }
            
        // Simple, direct implementation
        return string.Join(" ", input.Split(
            new[] { ' ', '\t', '\r', '\n' }, 
            StringSplitOptions.RemoveEmptyEntries));
    }
}

// If you really need multiple strategies later, you can refactor:
namespace MyApp.Services.Advanced;

// Only create abstractions when you have a real need
public class TextTransformationService
{
    private readonly Dictionary<string, Func<string, string>> transformations;
    
    public TextTransformationService()
    {
        // Only abstract when you have multiple real implementations
        this.transformations = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["upper"] = s => s.ToUpperInvariant(),
            ["lower"] = s => s.ToLowerInvariant(),
            ["title"] = s => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower()),
            ["reverse"] = s => new string(s.Reverse().ToArray())
        };
    }
    
    public string Transform(string input, string transformationType)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        ArgumentException.ThrowIfNullOrEmpty(transformationType);
        
        if (!this.transformations.TryGetValue(transformationType, out Func<string, string>? transformation))
        {
                throw new ArgumentException($"Unknown transformation type: {transformationType}", nameof(transformationType));
        }
            
        return transformation(input);
    }
}
```

#### 6. Writing Production Code Without Tests

```csharp
namespace MyApp.AntiPatterns;

using System.Net.Mail;

// ❌ BAD: Implementation without tests
public class BadEmailService
{
    private readonly SmtpClient smtpClient;
    
    public BadEmailService()
    {
        // Hard-coded configuration
        this.smtpClient = new SmtpClient("smtp.example.com")
        {
            Port = 587,
            Credentials = new System.Net.NetworkCredential("user", "pass"),
            EnableSsl = true
        };
    }
    
    public void SendEmail(string to, string subject, string body)
    {
        // Complex implementation written without tests
        MailMessage message = new();
        message.To.Add(to);
        message.Subject = subject;
        message.Body = body;
        message.From = new MailAddress("noreply@example.com");
        message.IsBodyHtml = true;
        
        // Can't test this - directly sends email!
        this.smtpClient.Send(message);
        
        // No error handling, no logging, no validation
    }
}

// ✅ GOOD: Test-driven implementation
namespace MyApp.Tests;

[TestClass]
public class EmailServiceTests
{
    private IEmailClient emailClient = null!;
    private ILogger<EmailService> logger = null!;
    private EmailService service = null!;
    
    [TestInitialize]
    public void Setup()
    {
        this.emailClient = Substitute.For<IEmailClient>();
        this.logger = Substitute.For<ILogger<EmailService>>();
        this.service = new EmailService(this.emailClient, this.logger);
    }
    
    [TestMethod]
    public async Task SendAsync_WithValidRecipient_SendsSuccessfully()
    {
        // Arrange
        string recipient = "user@example.com";
        string subject = "Test Subject";
        string body = "Test Body";
        
        this.emailClient.SendAsync(Arg.Any<EmailMessage>()).Returns(new EmailResult { Success = true, MessageId = "MSG123" });
        
        // Act
        EmailResult result = await this.service.SendAsync(recipient, subject, body);
        
        // Assert
        result.Success.ShouldBeTrue();
        result.MessageId.ShouldBe("MSG123");
        
        await this.emailClient.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => 
                m.To.Single() == recipient && 
                m.Subject == subject &&
                m.Body == body));
    }
    
    [TestMethod]
    public async Task SendAsync_WithInvalidEmail_ThrowsValidationException()
    {
        // Act & Assert
        await Should.ThrowAsync<ValidationException>(async () => await this.service.SendAsync("invalid-email", "Subject", "Body"));
            
        await this.emailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>());
    }
    
    [TestMethod]
    public async Task SendAsync_WhenEmailClientFails_ReturnsFailureResult()
    {
        // Arrange
        this.emailClient.SendAsync(Arg.Any<EmailMessage>()).Returns(new EmailResult { Success = false, Error = "SMTP connection failed" });
        
        // Act
        EmailResult result = await this.service.SendAsync("user@example.com", "Subject", "Body");
        
        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("SMTP connection failed");
    }
}

// Implementation driven by tests
namespace MyApp.Services;

public class EmailService(IEmailClient emailClient, ILogger<EmailService> logger)
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public async Task<EmailResult> SendAsync(string to, string subject, string body)
    {
        // Validation added because tests demanded it
        if (!IsValidEmail(to))
            throw new ValidationException($"Invalid email address: {to}");
            
        ArgumentException.ThrowIfNullOrEmpty(subject);
        ArgumentException.ThrowIfNullOrEmpty(body);
        
        EmailMessage message = new()
        {
            To = [to],
            Subject = subject,
            Body = body,
            From = "noreply@example.com", // Would come from configuration
            IsHtml = false,
            SentAt = DateTime.UtcNow
        };
        
        try
        {
            EmailResult result = await emailClient.SendAsync(message);
            
            if (result.Success)
            {
                logger.LogInformation(
                    "Email sent successfully to {Recipient} with message ID {MessageId}",
                    to, result.MessageId);
            }
            else
            {
                logger.LogWarning(
                    "Failed to send email to {Recipient}: {Error}",
                    to, result.Error);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while sending email to {Recipient}", to);
            return new EmailResult 
            { 
                Success = false, 
                Error = "An error occurred while sending the email" 
            };
        }
    }
    
    private static bool IsValidEmail(string email) => EmailRegex.IsMatch(email);
}

// Clean abstractions
public interface IEmailClient
{
    Task<EmailResult> SendAsync(EmailMessage message);
}

public record EmailMessage
{
    public required IReadOnlyList<string> To { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required string From { get; init; }
    public bool IsHtml { get; init; }
    public DateTime SentAt { get; init; }
}

public record EmailResult
{
    public required bool Success { get; init; }
    public string? MessageId { get; init; }
    public string? Error { get; init; }
}
```