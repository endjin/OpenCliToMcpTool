# .NET / C# Testing Frameworks Guidelines

## Overview

This specification explores modern .NET testing frameworks, focusing on Microsoft.Testing.Platform (MTP), Shouldly assertion framework, and NSubstitute mocking framework. It provides comprehensive guidance for building robust, maintainable test suites using these tools together.

## Table of Contents

1. [Microsoft.Testing.Platform](#microsoft-testing-platform)
2. [Shouldly Assertion Framework](#shouldly-assertion-framework)
3. [NSubstitute Mocking Framework](#nsubstitute-mocking-framework)
4. [Integration Patterns](#integration-patterns)
5. [Migration Strategies](#migration-strategies)
6. [Best Practices](#best-practices)
7. [Real-World Examples](#real-world-examples)

## Microsoft.Testing.Platform

### Introduction

Microsoft.Testing.Platform (MTP) represents a paradigm shift in .NET testing, moving from traditional test runners to standalone executable tests. Released in 2024, it offers superior performance, modern architecture, and native AOT support.

### Architecture

#### Core Design Principles

1. **Determinism**: Consistent results across all environments
2. **Runtime Transparency**: No interference with test code
3. **Performance**: Minimal overhead and optimized execution
4. **Extensibility**: Rich extension model
5. **Zero Dependencies**: Standalone executables

#### Architecture Overview

```
┌─────────────────────────────────────────────┐
│            Test Application                  │
│  ┌────────────────────────────────────────┐ │
│  │        Microsoft.Testing.Platform      │ │
│  │  ┌──────────────┐  ┌────────────────┐ │ │
│  │  │ Test Engine  │  │   Extensions    │ │ │
│  │  └──────────────┘  └────────────────┘ │ │
│  └────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────┐ │
│  │     Test Framework (MSTest/xUnit)      │ │
│  └────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────┐ │
│  │            Your Test Code               │ │
│  └────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
```

### Configuration

#### Basic Setup

**MSTest Integration:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <EnableMSTestRunner>true</EnableMSTestRunner>
    <GenerateProgramFile>false</GenerateProgramFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MSTest" Version="3.2.0" />
    <PackageReference Include="Microsoft.Testing.Platform.MSBuild" Version="1.1.0" />
  </ItemGroup>
</Project>
```

**xUnit Integration:**
```xml
<PropertyGroup>
  <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="xunit" Version="2.6.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.6.0" />
</ItemGroup>
```

#### Advanced Configuration

```csharp
// Program.cs for custom configuration
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions.TrxReport;

var builder = await TestApplication.CreateBuilderAsync(args);

// Configure test host
builder.TestHost.AddDataConsumer(new TrxReportDataConsumer());
builder.TestHost.AddTestApplicationLifecycleCallbacks(new MyLifecycleCallbacks());

// Configure extensions
builder.Extensions.AddTrxReportProvider();
builder.Extensions.AddCodeCoverageProvider();

var app = await builder.BuildAsync();
return await app.RunAsync();
```

### Extension Model

#### Creating Custom Extensions

```csharp
public interface ITestReporter : IDataConsumer
{
    void ReportTestResult(TestNode test, TestResult result);
}

public class CustomTestReporter : ITestReporter, IDataConsumer
{
    public Type[] DataTypesConsumed => new[] { typeof(TestNode) };
    
    public async Task ConsumeAsync(
        IDataProducer dataProducer, 
        IData data, 
        CancellationToken cancellationToken)
    {
        if (data is TestNode testNode)
        {
            // Custom reporting logic
            await ReportToExternalSystem(testNode);
        }
    }
}

// Registration
builder.TestHost.AddDataConsumer(new CustomTestReporter());
```

#### Capability System

```csharp
public interface IParallelizationCapability : ICapability
{
    int MaxDegreeOfParallelism { get; }
}

public class ParallelizationExtension : ITestHostExtension, IParallelizationCapability
{
    public int MaxDegreeOfParallelism => Environment.ProcessorCount;
    
    public void Enable(ITestHostBuilder builder)
    {
        builder.Services.AddSingleton<IParallelizationCapability>(this);
    }
}
```

### Command Line Interface

```bash
# Direct execution
./MyTests.exe

# With filters
./MyTests.exe --filter "FullyQualifiedName~Integration"

# Parallel execution
./MyTests.exe --parallel --parallel-workers 4

# With custom settings
./MyTests.exe --settings test.runsettings

# Generate report
./MyTests.exe --report-trx --report-trx-filename results.trx
```

## Shouldly Assertion Framework

### Introduction

Shouldly transforms test assertions into readable, natural language statements while providing exceptional error messages that include context about what went wrong.

### Core Assertions

#### Basic Assertions

```csharp
// Equality
actual.ShouldBe(expected);
actual.ShouldNotBe(expected);

// Null checks
value.ShouldBeNull();
value.ShouldNotBeNull();

// Boolean
condition.ShouldBeTrue();
condition.ShouldBeFalse();

// Type checking
obj.ShouldBeOfType<ExpectedType>();
obj.ShouldBeAssignableTo<BaseType>();

// Comparisons
number.ShouldBeGreaterThan(5);
number.ShouldBeLessThanOrEqualTo(10);
number.ShouldBeInRange(1, 10);
```

#### String Assertions

```csharp
// Content
text.ShouldContain("substring");
text.ShouldStartWith("prefix");
text.ShouldEndWith("suffix");
text.ShouldMatch("^[A-Z].*");

// Case-insensitive
text.ShouldBe("EXPECTED", StringCompareShould.IgnoreCase);

// Empty/whitespace
text.ShouldBeNullOrEmpty();
text.ShouldNotBeNullOrWhiteSpace();

// Multi-line strings
actual.ShouldBe(@"Line 1
Line 2
Line 3", customMessage: "Multi-line comparison failed");
```

#### Collection Assertions

```csharp
// Count
collection.ShouldBeEmpty();
collection.ShouldNotBeEmpty();
collection.Count.ShouldBe(5);

// Contains
collection.ShouldContain(item);
collection.ShouldContain(x => x.Id == 123);
collection.ShouldNotContain(item);

// All/Any
collection.ShouldAllBe(x => x > 0);
collection.ShouldContain(x => x.IsActive);

// Ordering
collection.ShouldBeInOrder();
collection.ShouldBeInOrder(SortDirection.Descending);

// Set operations
collectionA.ShouldBe(collectionB, ignoreOrder: true);
collection.ShouldBeUnique();

// Complex comparisons
actualList.ShouldBe(expectedList, (actual, expected) => 
    actual.Id == expected.Id && 
    actual.Name == expected.Name);
```

#### Exception Assertions

```csharp
// Basic exception
Should.Throw<ArgumentException>(() => MethodThatThrows());

// With return value
var exception = Should.Throw<InvalidOperationException>(() => 
{
    return service.Process(invalidInput);
});
exception.Message.ShouldContain("Invalid state");

// Async exceptions
await Should.ThrowAsync<HttpRequestException>(async () => 
{
    await client.GetAsync("https://invalid-url");
});

// No exception
Should.NotThrow(() => SafeMethod());
```

#### Async Assertions

```csharp
// Task assertions
var task = service.ProcessAsync();
await task.ShouldCompleteIn(TimeSpan.FromSeconds(5));

// Async value assertions
var result = await service.GetValueAsync();
result.ShouldBe(expected);

// Async exception handling
await Should.ThrowAsync<TimeoutException>(
    service.LongRunningOperationAsync());
```

### Advanced Features

#### Custom Messages

```csharp
actual.ShouldBe(expected, customMessage: "Failed because {0} != {1}", actual, expected);

// With lazy evaluation
actual.ShouldBe(expected, () => $"Complex message: {GenerateDebugInfo()}");
```

#### Tolerance and Approximation

```csharp
// Floating point
Math.PI.ShouldBe(3.14159, tolerance: 0.00001);

// DateTime
DateTime.Now.ShouldBe(expected, TimeSpan.FromSeconds(1));

// Custom equality
actual.ShouldBe(expected, new CustomEqualityComparer());
```

#### Shouldly Configuration

```csharp
// Global configuration
Shouldly.Configuration.DefaultFloatingPointTolerance = 0.001;
Shouldly.Configuration.DefaultTaskTimeout = TimeSpan.FromSeconds(10);

// Per-test configuration
using (ShouldlyConfiguration.PushDefaultTaskTimeout(TimeSpan.FromMinutes(1)))
{
    await LongRunningTest();
}
```

### Error Message Examples

```csharp
// Traditional assertion:
// Expected: 5 but was: 3

// Shouldly assertion:
// numbers.Count(x => x > 10)
//     should be
// 5
//     but was
// 3

// With context:
// user.Orders.Where(o => o.Status == "Pending").Count()
//     should be
// 0
//     but was
// 2
// Additional Info:
// Pending orders found: Order #123, Order #456
```

## NSubstitute Mocking Framework

### Introduction

NSubstitute provides a friendly syntax for creating test doubles in .NET, emphasizing clean, readable test code without sacrificing functionality.

### Basic Mocking

#### Creating Substitutes

```csharp
// Interface substitute
var calculator = Substitute.For<ICalculator>();

// Class substitute (virtual members only)
var service = Substitute.For<DatabaseService>();

// Multiple interfaces
var multi = Substitute.For<IRepository, IDisposable>();

// With constructor arguments
var logger = Substitute.For<Logger>(LogLevel.Debug);
```

#### Setting Return Values

```csharp
// Simple returns
calculator.Add(1, 2).Returns(3);
calculator.Mode.Returns("Scientific");

// Different returns for different calls
calculator.Add(Arg.Any<int>(), Arg.Any<int>())
    .Returns(x => (int)x[0] + (int)x[1]);

// Sequential returns
calculator.GetNext()
    .Returns(1, 2, 3); // Returns 1, then 2, then 3

// Conditional returns
calculator.Divide(Arg.Any<int>(), Arg.Is<int>(x => x != 0))
    .Returns(x => (int)x[0] / (int)x[1]);
calculator.Divide(Arg.Any<int>(), 0)
    .Returns(x => throw new DivideByZeroException());
```

### Argument Matching

#### Basic Matchers

```csharp
// Any value
service.Process(Arg.Any<string>()).Returns(true);

// Specific condition
service.Process(Arg.Is<string>(x => x.Length > 5)).Returns(true);

// Not equal
service.Process(Arg.Is<string>(x => x != "invalid")).Returns(true);

// Multiple conditions
repository.Find(
    Arg.Is<int>(id => id > 0),
    Arg.Is<string>(name => !string.IsNullOrEmpty(name))
).Returns(new User());
```

#### Advanced Matchers

```csharp
// Custom matcher
public class EvenNumberMatcher : IArgumentMatcher<int>
{
    public bool IsSatisfiedBy(int argument) => argument % 2 == 0;
}

calculator.Double(Arg.Matches(new EvenNumberMatcher())).Returns(x => (int)x[0] * 2);

// Composite matchers
service.ProcessBatch(
    Arg.Is<List<string>>(list => list.Count > 0 && list.All(s => !string.IsNullOrEmpty(s)))
).Returns(true);

// Out and ref parameters
string output;
parser.TryParse("123", out output).Returns(x => 
{
    x[1] = "parsed";
    return true;
});
```

### Callbacks and Actions

```csharp
// Execute code when called
var callCount = 0;
calculator.Add(Arg.Any<int>(), Arg.Any<int>())
    .Returns(x => (int)x[0] + (int)x[1])
    .AndDoes(x => callCount++);

// Capture arguments
var capturedIds = new List<int>();
repository.Delete(Arg.Do<int>(id => capturedIds.Add(id)));

// Throw exceptions
service.Process(Arg.Is<string>(x => x == "error"))
    .Returns(x => throw new InvalidOperationException("Processing failed"));

// Async callbacks
await service.ProcessAsync(Arg.Any<string>())
    .Returns(Task.CompletedTask)
    .AndDoes(async x => await LogAsync($"Processing: {x[0]}"));
```

### Received Calls Verification

```csharp
// Verify exact calls
calculator.Received(1).Add(1, 2);
calculator.DidNotReceive().Divide(Arg.Any<int>(), 0);

// Verify any calls
calculator.ReceivedWithAnyArgs().Add(default, default);

// Verify call order
Received.InOrder(() =>
{
    service.Initialize();
    service.Process("data");
    service.Cleanup();
});

// Verify property access
var mode = calculator.Mode;
calculator.Received().Mode;

// Clear received calls
calculator.ClearReceivedCalls();
```

### Advanced Features

#### Partial Mocks

```csharp
var partialSub = Substitute.ForPartsOf<DatabaseService>();
partialSub.Configure().SaveToDatabase(Arg.Any<string>()).Returns(true);
// Other methods use real implementation
```

#### Auto-Mocking

```csharp
// Returns default values automatically
calculator.Add(1, 2); // Returns 0 (default for int)
service.GetUser(123); // Returns null (default for reference type)

// Configure auto-values
var sub = Substitute.For<IService>();
sub.ReturnsForAll<string>("default");
sub.GetName(); // Returns "default"
```

#### Raising Events

```csharp
public interface IEngine
{
    event EventHandler<EngineEventArgs> Started;
    void Start();
}

var engine = Substitute.For<IEngine>();
var eventRaised = false;
engine.Started += (sender, args) => eventRaised = true;

// Raise event
engine.Started += Raise.EventWith(new EngineEventArgs());
eventRaised.ShouldBeTrue();
```

## Integration Patterns

### Project Setup

#### Modern Test Project Structure

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <EnableMSTestRunner>true</EnableMSTestRunner>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Testing Platform -->
    <PackageReference Include="MSTest" Version="3.2.0" />
    <PackageReference Include="Microsoft.Testing.Platform.MSBuild" Version="1.1.0" />
    
    <!-- Assertion Framework -->
    <PackageReference Include="Shouldly" Version="4.2.1" />
    
    <!-- Mocking Framework -->
    <PackageReference Include="NSubstitute" Version="5.1.0" />
    
    <!-- Additional Tools -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
  </ItemGroup>
</Project>
```

### Base Test Class Pattern

```csharp
public abstract class TestBase
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected ITestOutputHelper Output { get; }
    
    protected TestBase(ITestOutputHelper output)
    {
        Output = output;
        ServiceProvider = BuildServiceProvider();
    }
    
    protected virtual IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }
    
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder => 
        {
            builder.AddDebug();
            builder.AddTestOutput(Output);
        });
        
        // Add common services
        services.AddScoped<IDateTimeProvider, TestDateTimeProvider>();
    }
    
    protected T GetService<T>() => ServiceProvider.GetRequiredService<T>();
    
    protected T CreateSubstitute<T>() where T : class
    {
        var substitute = Substitute.For<T>();
        ConfigureSubstitute(substitute);
        return substitute;
    }
    
    protected virtual void ConfigureSubstitute<T>(T substitute) where T : class
    {
        // Override in derived classes for common setup
    }
}
```

### Integration Test Pattern

```csharp
[TestClass]
public class OrderServiceIntegrationTests : TestBase
{
    private readonly IOrderService _orderService;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly INotificationService _notificationService;
    
    public OrderServiceIntegrationTests()
    {
        _orderRepository = CreateSubstitute<IOrderRepository>();
        _paymentGateway = CreateSubstitute<IPaymentGateway>();
        _notificationService = CreateSubstitute<INotificationService>();
        
        _orderService = new OrderService(
            _orderRepository,
            _paymentGateway,
            _notificationService,
            GetService<ILogger<OrderService>>());
    }
    
    [TestMethod]
    public async Task ProcessOrder_ValidOrder_CompletesSuccessfully()
    {
        // Arrange
        var order = new Order
        {
            Id = 123,
            Total = 99.99m,
            CustomerId = "cust-456"
        };
        
        _orderRepository.GetAsync(123).Returns(order);
        _paymentGateway.ProcessPaymentAsync(Arg.Any<PaymentRequest>())
            .Returns(new PaymentResult { Success = true, TransactionId = "txn-789" });
        
        // Act
        var result = await _orderService.ProcessOrderAsync(123);
        
        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(OrderStatus.Completed);
        result.TransactionId.ShouldBe("txn-789");
        
        // Verify interactions
        await _orderRepository.Received(1).GetAsync(123);
        await _paymentGateway.Received(1).ProcessPaymentAsync(
            Arg.Is<PaymentRequest>(req => 
                req.Amount == 99.99m && 
                req.CustomerId == "cust-456"));
        await _notificationService.Received(1).SendOrderConfirmationAsync(order);
    }
    
    [TestMethod]
    public async Task ProcessOrder_PaymentFails_RollsBackOrder()
    {
        // Arrange
        var order = new Order { Id = 123, Total = 99.99m };
        
        _orderRepository.GetAsync(123).Returns(order);
        _paymentGateway.ProcessPaymentAsync(Arg.Any<PaymentRequest>())
            .Returns(new PaymentResult { Success = false, ErrorMessage = "Insufficient funds" });
        
        // Act & Assert
        var exception = await Should.ThrowAsync<PaymentException>(
            _orderService.ProcessOrderAsync(123));
        
        exception.Message.ShouldContain("Insufficient funds");
        
        // Verify rollback
        await _orderRepository.Received(1).UpdateStatusAsync(123, OrderStatus.PaymentFailed);
        await _notificationService.DidNotReceive().SendOrderConfirmationAsync(Arg.Any<Order>());
    }
}
```

### Unit Test Pattern

```csharp
[TestClass]
public class PriceCalculatorTests
{
    private readonly PriceCalculator _calculator;
    private readonly ITaxService _taxService;
    private readonly IDiscountService _discountService;
    
    public PriceCalculatorTests()
    {
        _taxService = Substitute.For<ITaxService>();
        _discountService = Substitute.For<IDiscountService>();
        _calculator = new PriceCalculator(_taxService, _discountService);
    }
    
    [TestMethod]
    [DataRow(100, 0.1, 10)]
    [DataRow(50, 0.2, 10)]
    [DataRow(200, 0.15, 30)]
    public void CalculateTotal_AppliesDiscountAndTax(
        decimal basePrice, 
        decimal taxRate, 
        decimal discount)
    {
        // Arrange
        _taxService.GetTaxRate(Arg.Any<string>()).Returns(taxRate);
        _discountService.GetDiscount(Arg.Any<string>()).Returns(discount);
        
        // Act
        var total = _calculator.CalculateTotal(basePrice, "US", "SAVE10");
        
        // Assert
        var expectedTotal = (basePrice - discount) * (1 + taxRate);
        total.ShouldBe(expectedTotal, tolerance: 0.01m);
    }
    
    [TestMethod]
    public void CalculateTotal_NegativePrice_ThrowsException()
    {
        // Arrange
        var negativePrice = -50m;
        
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(
            () => _calculator.CalculateTotal(negativePrice, "US", null));
        
        exception.ParamName.ShouldBe("basePrice");
        exception.Message.ShouldContain("must be non-negative");
    }
}
```

### Parallel Test Execution

```csharp
// Configure parallel execution in test assembly
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
[assembly: DoNotParallelize] // For specific test classes that shouldn't run in parallel

[TestClass]
[DoNotParallelize] // Database integration tests
public class DatabaseIntegrationTests
{
    // Tests that modify shared state
}

[TestClass]
public class CalculatorTests // Safe for parallel execution
{
    // Isolated unit tests
}
```

## Migration Strategies

### From xUnit to Microsoft.Testing.Platform

```csharp
// Before (xUnit)
public class CalculatorTests
{
    [Fact]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var result = Calculator.Add(2, 3);
        Assert.Equal(5, result);
    }
    
    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(2, 3, 5)]
    public void Add_Various_ReturnsSum(int a, int b, int expected)
    {
        var result = Calculator.Add(a, b);
        Assert.Equal(expected, result);
    }
}

// After (MSTest with MTP + Shouldly)
[TestClass]
public class CalculatorTests
{
    [TestMethod]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var result = Calculator.Add(2, 3);
        result.ShouldBe(5);
    }
    
    [TestMethod]
    [DataRow(1, 1, 2)]
    [DataRow(2, 3, 5)]
    public void Add_Various_ReturnsSum(int a, int b, int expected)
    {
        var result = Calculator.Add(a, b);
        result.ShouldBe(expected);
    }
}
```

### From Moq to NSubstitute

```csharp
// Before (Moq)
var mockService = new Mock<IDataService>();
mockService.Setup(x => x.GetData(It.IsAny<int>()))
    .Returns<int>(id => $"Data-{id}");
mockService.Setup(x => x.SaveData(It.IsAny<string>()))
    .Throws<InvalidOperationException>();

var service = mockService.Object;
var result = service.GetData(123);

mockService.Verify(x => x.GetData(123), Times.Once);

// After (NSubstitute)
var service = Substitute.For<IDataService>();
service.GetData(Arg.Any<int>())
    .Returns(x => $"Data-{x[0]}");
service.SaveData(Arg.Any<string>())
    .Returns(x => throw new InvalidOperationException());

var result = service.GetData(123);

service.Received(1).GetData(123);
```

## Best Practices

### 1. Test Organization

```csharp
// Follow AAA pattern with clear sections
[TestMethod]
public async Task OrderService_ProcessPayment_Success()
{
    // Arrange
    var order = CreateTestOrder();
    var paymentResult = CreateSuccessfulPaymentResult();
    
    _paymentGateway
        .ProcessAsync(Arg.Any<PaymentRequest>())
        .Returns(paymentResult);
    
    // Act
    var result = await _orderService.ProcessPaymentAsync(order);
    
    // Assert
    result.Success.ShouldBeTrue();
    result.TransactionId.ShouldNotBeNullOrEmpty();
    
    await _paymentGateway
        .Received(1)
        .ProcessAsync(Arg.Is<PaymentRequest>(req => req.Amount == order.Total));
}
```

### 2. Assertion Best Practices

```csharp
// Be specific with assertions
// Bad
result.ShouldNotBeNull();

// Good
result.ShouldNotBeNull();
result.Status.ShouldBe(OrderStatus.Completed);
result.Items.Count.ShouldBe(3);
result.Total.ShouldBe(99.99m);

// Use custom messages for complex assertions
result.Items.ShouldAllBe(
    item => item.Quantity > 0,
    customMessage: "All items must have positive quantity");
```

### 3. Mock Setup Guidelines

```csharp
// Configure only what's needed
// Bad - Over-mocking
var userService = Substitute.For<IUserService>();
userService.GetUser(Arg.Any<int>()).Returns(new User());
userService.GetUserName(Arg.Any<int>()).Returns("John");
userService.GetUserEmail(Arg.Any<int>()).Returns("john@example.com");
userService.IsUserActive(Arg.Any<int>()).Returns(true);

// Good - Mock only what's used
var userService = Substitute.For<IUserService>();
userService.GetUser(123).Returns(new User 
{ 
    Id = 123, 
    Name = "John", 
    Email = "john@example.com" 
});
```

### 4. Test Data Builders

```csharp
public class OrderBuilder
{
    private Order _order = new();
    
    public OrderBuilder WithId(int id)
    {
        _order.Id = id;
        return this;
    }
    
    public OrderBuilder WithCustomer(string customerId)
    {
        _order.CustomerId = customerId;
        return this;
    }
    
    public OrderBuilder WithItems(params OrderItem[] items)
    {
        _order.Items.AddRange(items);
        _order.Total = items.Sum(i => i.Price * i.Quantity);
        return this;
    }
    
    public OrderBuilder WithStatus(OrderStatus status)
    {
        _order.Status = status;
        return this;
    }
    
    public Order Build() => _order;
    
    public static implicit operator Order(OrderBuilder builder) => builder.Build();
}

// Usage
var order = new OrderBuilder()
    .WithId(123)
    .WithCustomer("cust-456")
    .WithItems(
        new OrderItem { ProductId = 1, Quantity = 2, Price = 10.00m },
        new OrderItem { ProductId = 2, Quantity = 1, Price = 25.00m })
    .WithStatus(OrderStatus.Pending);
```

### 5. Test Isolation

```csharp
[TestClass]
public class UserServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IUserService _userService;
    
    public UserServiceTests()
    {
        var services = new ServiceCollection();
        services.AddScoped<IUserRepository>(_ => Substitute.For<IUserRepository>());
        services.AddScoped<IUserService, UserService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _userService = _serviceProvider.GetRequiredService<IUserService>();
    }
    
    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
```

## Real-World Examples

### 1. API Integration Test

```csharp
[TestClass]
public class WeatherApiIntegrationTests
{
    private readonly IWeatherApiClient _apiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;
    
    public WeatherApiIntegrationTests()
    {
        _httpClient = Substitute.For<HttpClient>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);
        
        _apiClient = new WeatherApiClient(_httpClientFactory);
    }
    
    [TestMethod]
    public async Task GetWeather_ValidCity_ReturnsWeatherData()
    {
        // Arrange
        var city = "London";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"{
                ""temperature"": 15,
                ""conditions"": ""Cloudy"",
                ""humidity"": 65
            }")
        };
        
        _httpClient
            .SendAsync(Arg.Is<HttpRequestMessage>(req => 
                req.RequestUri.ToString().Contains(city)))
            .Returns(expectedResponse);
        
        // Act
        var weather = await _apiClient.GetWeatherAsync(city);
        
        // Assert
        weather.ShouldNotBeNull();
        weather.Temperature.ShouldBe(15);
        weather.Conditions.ShouldBe("Cloudy");
        weather.Humidity.ShouldBe(65);
    }
    
    [TestMethod]
    public async Task GetWeather_ApiError_ThrowsWeatherApiException()
    {
        // Arrange
        _httpClient
            .SendAsync(Arg.Any<HttpRequestMessage>())
            .Returns(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        
        // Act & Assert
        var exception = await Should.ThrowAsync<WeatherApiException>(
            _apiClient.GetWeatherAsync("InvalidCity"));
        
        exception.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        exception.Message.ShouldContain("Weather API is unavailable");
    }
}
```

### 2. Domain Logic Test

```csharp
[TestClass]
public class ShoppingCartTests
{
    private readonly ShoppingCart _cart;
    private readonly IProductCatalog _catalog;
    private readonly IPricingService _pricingService;
    
    public ShoppingCartTests()
    {
        _catalog = Substitute.For<IProductCatalog>();
        _pricingService = Substitute.For<IPricingService>();
        _cart = new ShoppingCart(_catalog, _pricingService);
    }
    
    [TestMethod]
    public async Task AddItem_ValidProduct_AddsToCart()
    {
        // Arrange
        var product = new Product { Id = "P1", Name = "Laptop", Price = 999.99m };
        _catalog.GetProductAsync("P1").Returns(product);
        
        // Act
        await _cart.AddItemAsync("P1", 2);
        
        // Assert
        _cart.Items.Count.ShouldBe(1);
        _cart.Items[0].ProductId.ShouldBe("P1");
        _cart.Items[0].Quantity.ShouldBe(2);
        _cart.Items[0].UnitPrice.ShouldBe(999.99m);
    }
    
    [TestMethod]
    public async Task CalculateTotal_WithDiscounts_AppliesCorrectly()
    {
        // Arrange
        await SetupCartWithItems();
        
        _pricingService
            .CalculateDiscountAsync(Arg.Any<decimal>(), Arg.Any<string>())
            .Returns(info => (decimal)info[0] * 0.1m); // 10% discount
        
        // Act
        var total = await _cart.CalculateTotalAsync("SAVE10");
        
        // Assert
        total.Subtotal.ShouldBe(1500m);
        total.Discount.ShouldBe(150m);
        total.Tax.ShouldBe(135m); // 10% of discounted price
        total.GrandTotal.ShouldBe(1485m);
        
        await _pricingService
            .Received(1)
            .CalculateDiscountAsync(1500m, "SAVE10");
    }
}
```

### 3. Event-Driven Test

```csharp
[TestClass]
public class OrderEventHandlerTests
{
    private readonly IEventBus _eventBus;
    private readonly IOrderRepository _repository;
    private readonly OrderEventHandler _handler;
    
    public OrderEventHandlerTests()
    {
        _eventBus = Substitute.For<IEventBus>();
        _repository = Substitute.For<IOrderRepository>();
        _handler = new OrderEventHandler(_eventBus, _repository);
    }
    
    [TestMethod]
    public async Task Handle_OrderPlaced_PublishesDownstreamEvents()
    {
        // Arrange
        var orderPlacedEvent = new OrderPlacedEvent
        {
            OrderId = 123,
            CustomerId = "CUST456",
            Items = new[] { new OrderItem { ProductId = "P1", Quantity = 2 } }
        };
        
        var capturedEvents = new List<IEvent>();
        await _eventBus
            .PublishAsync(Arg.Do<IEvent>(e => capturedEvents.Add(e)));
        
        // Act
        await _handler.HandleAsync(orderPlacedEvent);
        
        // Assert
        capturedEvents.Count.ShouldBe(2);
        
        var inventoryEvent = capturedEvents.OfType<InventoryReservationRequested>().First();
        inventoryEvent.OrderId.ShouldBe(123);
        inventoryEvent.Items.ShouldContain(i => i.ProductId == "P1" && i.Quantity == 2);
        
        var paymentEvent = capturedEvents.OfType<PaymentRequested>().First();
        paymentEvent.OrderId.ShouldBe(123);
        paymentEvent.CustomerId.ShouldBe("CUST456");
    }
}
```

## Performance Testing

### Benchmark Integration

```csharp
[TestClass]
public class PerformanceTests
{
    [TestMethod]
    [Timeout(5000)] // 5 second timeout
    public async Task BatchProcessor_LargeDataset_CompletesWithinTimeout()
    {
        // Arrange
        var processor = new BatchProcessor();
        var items = GenerateLargeDataset(10000);
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        await processor.ProcessAsync(items);
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(3000);
    }
    
    [TestMethod]
    public void CacheService_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var cache = new ThreadSafeCache<string, int>();
        var tasks = new List<Task>();
        
        // Act
        for (int i = 0; i < 100; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(() =>
            {
                cache.Set($"key{taskId}", taskId);
                var value = cache.Get($"key{taskId}");
                value.ShouldBe(taskId);
            }));
        }
        
        // Assert
        Should.CompleteIn(() => Task.WhenAll(tasks), TimeSpan.FromSeconds(5));
        cache.Count.ShouldBe(100);
    }
}
```

## CI/CD Integration

### Azure DevOps Pipeline

```yaml
trigger:
  - main
  - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '8.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration $(buildConfiguration) --no-restore'

- task: DotNetCoreCLI@2
  displayName: 'Run tests'
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--configuration $(buildConfiguration) --no-build --logger trx --collect:"XPlat Code Coverage"'

- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
    mergeTestResults: true

- task: PublishCodeCoverageResults@1
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'
```

### GitHub Actions

```yaml
name: .NET Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Test
      run: dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage"
      
    - name: Test Report
      uses: dorny/test-reporter@v1
      if: success() || failure()
      with:
        name: Test Results
        path: '**/test-results.trx'
        reporter: dotnet-trx
        
    - name: Code Coverage Report
      uses: irongut/CodeCoverageSummary@v1.3.0
      with:
        filename: '**/coverage.cobertura.xml'
        badge: true
        format: markdown
        output: both
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Tests Not Discovered

```csharp
// Ensure test class is public and has TestClass attribute
[TestClass] // Required for MSTest
public class MyTests // Must be public
{
    [TestMethod] // Required for each test
    public void Test1() { }
}
```

#### 2. Async Test Deadlocks

```csharp
// Bad - Can cause deadlock
[TestMethod]
public void Test()
{
    var result = AsyncMethod().Result; // Avoid .Result
}

// Good
[TestMethod]
public async Task Test()
{
    var result = await AsyncMethod();
}
```

#### 3. NSubstitute Argument Matching Issues

```csharp
// Bad - Won't work with complex types
service.Process(complexObject).Returns(true);

// Good - Use argument matchers
service.Process(Arg.Is<ComplexType>(x => 
    x.Id == complexObject.Id && 
    x.Name == complexObject.Name)).Returns(true);
```

#### 4. Shouldly Message Formatting

```csharp
// Configure Shouldly for better output
Shouldly.Configuration.DefaultFloatingPointTolerance = 0.0001d;
Shouldly.Configuration.DefaultTaskTimeout = TimeSpan.FromSeconds(10);

// Custom diff tool
Shouldly.Configuration.DiffTools.RegisterDiffTool(
    new DiffTool("BeyondCompare", 
    "/usr/bin/bcompare", 
    "\"{0}\" \"{1}\""));
```

## Conclusion

The combination of Microsoft.Testing.Platform, Shouldly, and NSubstitute provides a modern, efficient, and developer-friendly testing stack for .NET applications. This specification provides the foundation for building robust test suites that are:

- **Fast**: Leveraging MTP's performance optimizations
- **Readable**: Using Shouldly's natural assertions
- **Maintainable**: With NSubstitute's clean mocking syntax
- **Scalable**: Supporting large test suites with parallel execution
- **Integrated**: Working seamlessly with modern CI/CD pipelines

By following these patterns and practices, teams can create test suites that effectively validate application behavior while remaining easy to understand and maintain.