
# C# / .NET Code Style Guidelines

## Preferred Tools:

- **Language**: .NET 9.0 / C#
- **Testing**: Microsoft.Testing.Platform / Shouldly / NSubstitute
- **State Management**: Prefer immutable patterns
- **Preferred NuGet Package** Use Spectre.Console and Spectre.Cli for CLI apps and Spectre.IO for any file system operations

## Functional Programming

Follow a "functional light" approach:

- **No data mutation** - work with immutable data structures
- **Pure functions** wherever possible
- **Composition** as the primary mechanism for code reuse
- Avoid heavy FP abstractions (no need for complex monads or pipe/compose patterns) unless there is a clear advantage to using them
- No Comments in Code - Code should be self-documenting through clear naming and structure. Comments indicate that the code itself is not clear enough.

## Code Structure

- **No nested if/else statements** - use early returns, guard clauses, or composition
- **Avoid deep nesting** in general (max 2 levels)
- Keep functions small and focused on a single responsibility
- Prefer flat, readable code over clever abstractions

## Modern C# / .NET 9 Idioms Reference Guide

## **Key Guidelines**

1. **Prefer expressions over statements** - Use switch expressions, throw expressions, etc.
2. **Embrace immutability** - Use records, init properties, and immutable collections
3. **Reduce boilerplate** - Use target-typed new, collection expressions, primary constructors
4. **Improve readability** - Use file-scoped namespaces, raw string literals, pattern matching
5. **Be null-aware** - Use nullable reference types, null-coalescing operators, null-conditional operators
6. **Use modern collections** - Prefer frozen collections for lookups, collection expressions for initialization
7. **Optimize for performance** - Use ValueTask, spans, UTF-8 literals where appropriate

## **Collection Expressions (C# 12)**
```csharp
namespace MyApp.Examples;

public class CollectionExpressionExamples
{
    public void DemonstrateCollectionExpressions()
    {
        // ✅ Modern - Collection expressions
        List<int> numbers = [1, 2, 3, 4, 5];
        int[] array = [1, 2, 3];
        Span<int> span = [1, 2, 3];
        ReadOnlySpan<int> readOnlySpan = [1, 2, 3];
        
        // Works with any collection type
        HashSet<string> uniqueNames = ["Alice", "Bob", "Charlie"];
        ImmutableArray<decimal> prices = [19.99m, 29.99m, 39.99m];
        
        // ✅ Spread syntax
        int[] first = [1, 2];
        int[] second = [3, 4];
        int[] combined = [..first, ..second]; // [1, 2, 3, 4]
        
        // Combining with individual elements
        int[] moreNumbers = [0, ..first, 2.5, ..second, 5]; // Note: would need to be double[] for 2.5
        
        // ❌ Old way - avoid these patterns
        List<int> oldNumbers = new List<int> { 1, 2, 3, 4, 5 };
        int[] oldArray = new int[] { 1, 2, 3 };
        // Especially avoid using var with old syntax
        var avoidThis = new List<int> { 1, 2, 3 };
    }
    
    // Collection expressions in method parameters
    public int Sum(IEnumerable<int> values) => values.Sum();
    
    public void UseSum()
    {
        int total = Sum([1, 2, 3, 4, 5]); // Clean and concise
    }
}
```

## **Target-Typed New (C# 9)**
```csharp
namespace MyApp.Examples;

public class TargetTypedNewExamples
{
    public void DemonstrateTargetTypedNew()
    {
        // ✅ Modern - Target-typed new
        Dictionary<string, int> scores = new();
        List<Person> people = new();
        Person person = new("John", "Doe");
        
        // Complex types benefit greatly
        Dictionary<string, List<Product>> productsByCategory = new();
        ConcurrentDictionary<int, Customer> customerCache = new();
        
        // ❌ Old way - avoid these
        Dictionary<string, int> oldScores = new Dictionary<string, int>();
        // Especially avoid var with explicit construction
        var avoidVar = new Dictionary<string, int>();
    }
    
    // Target-typed new in returns
    public ApiResponse<Customer> GetCustomer(int id)
    {
        Customer? customer = FindCustomerById(id);
        if (customer == null)
        {
            return new()
            {
                Success = false,
                Error = "Customer not found",
                Data = null
            };
        }
        
        return new()
        {
            Success = true,
            Error = null,
            Data = customer
        };
    }
    
    private Customer? FindCustomerById(int id) => null; // Stub
}

public record Person(string FirstName, string LastName);
public record ApiResponse<T>
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public T? Data { get; init; }
}
```

## **Records and Immutability (C# 9+)**
```csharp
namespace MyApp.Domain;

// ✅ Modern - Records with positional syntax
public record Person(string FirstName, string LastName, int Age)
{
    // Can still add validation
    public string FullName => $"{FirstName} {LastName}";
    
    // Additional properties with init
    public DateOnly? DateOfBirth { get; init; }
}

// Example usage
public class RecordExamples
{
    public void DemonstrateRecords()
    {
        // ✅ With expressions for non-destructive mutation
        Person person = new("John", "Doe", 30);
        Person olderPerson = person with { Age = 31 };
        Person movedPerson = person with 
        { 
            LastName = "Smith",
            DateOfBirth = new DateOnly(1993, 5, 15)
        };
        
        // Records provide value equality
        Person duplicate = new("John", "Doe", 30);
        bool areEqual = person == duplicate; // true
    }
}

// ✅ Records with init properties and required modifier
public record Product
{
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required string Sku { get; init; }
    public DateTime Created { get; init; } = DateTime.UtcNow;
    public string? Description { get; init; }
    
    // Business logic in records
    public decimal CalculateDiscountPrice(decimal discountPercent)
    {
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent));
            
        return Price * (1 - discountPercent / 100);
    }
}

// ✅ Nested records for complex domain models
public record Address(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country = "USA");

public record Customer
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required Address BillingAddress { get; init; }
    public Address? ShippingAddress { get; init; }
    public ImmutableList<Order> Orders { get; init; } = [];
}
```

## **Required and Init Properties (C# 11)**
```csharp
// ✅ Modern - Required properties
public class User
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public DateTime Created { get; init; } = DateTime.UtcNow;
}

// Usage
var user = new User 
{ 
    Name = "John",     // Required by compiler
    Email = "john@example.com" // Required by compiler
};
```

## **Primary Constructors (C# 12)**
```csharp
// ✅ Modern - Primary constructors
public class CustomerService(ILogger<CustomerService> logger, IRepository repository)
{
    public async Task<Customer> GetCustomerAsync(int id)
    {
        logger.LogInformation("Getting customer {Id}", id);
        return await repository.GetAsync<Customer>(id);
    }
}

// ✅ With validation
public class Product(string name, decimal price)
{
    public string Name { get; } = !string.IsNullOrEmpty(name) 
        ? name 
        : throw new ArgumentException(nameof(name));
    
    public decimal Price { get; } = price >= 0 
        ? price 
        : throw new ArgumentException(nameof(price));
}
```

## **File-Scoped Namespaces (C# 10)**
```csharp
// ✅ Modern - File-scoped namespace
namespace MyApp.Services;

public class UserService
{
    // Implementation
}

// ❌ Old way
namespace MyApp.Services
{
    public class UserService
    {
        // Implementation
    }
}
```

## **Global Using Directives (C# 10)**
```csharp
// ✅ In GlobalUsings.cs or at top of program
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
```

## **Raw String Literals (C# 11)**
```csharp
// ✅ Modern - Raw string literals
string json = """
    {
        "name": "John Doe",
        "email": "john@example.com",
        "address": {
            "street": "123 Main St",
            "city": "Anytown"
        }
    }
    """;

// ✅ With interpolation
string template = $$"""
    Hello {{name}},
    Your order total is {{total:C}}.
    Order details: {{orderJson}}
    """;

// ❌ Old way with escaping
string json = "{\n  \"name\": \"John Doe\",\n  \"email\": \"john@example.com\"\n}";
```

## **Pattern Matching and Switch Expressions (C# 8+)**
```csharp
// ✅ Modern - Switch expressions
public string GetGrade(int score) => score switch
{
    >= 90 => "A",
    >= 80 => "B", 
    >= 70 => "C",
    >= 60 => "D",
    _ => "F"
};

// ✅ Pattern matching with is
if (obj is string { Length: > 0 } str)
{
    Console.WriteLine($"Non-empty string: {str}");
}

// ✅ Type patterns
public decimal CalculateArea(object shape) => shape switch
{
    Circle { Radius: var r } => Math.PI * r * r,
    Rectangle { Width: var w, Height: var h } => w * h,
    Square { Side: var s } => s * s,
    _ => throw new ArgumentException("Unknown shape")
};
```

## **Null-Coalescing Assignment (C# 8)**
```csharp
namespace MyApp.Examples;

public class NullCoalescingExamples
{
    private string? cachedValue;
    private Dictionary<string, object>? cache;
    
    public void DemonstrateNullCoalescingAssignment()
    {
        // ✅ Modern - Null-coalescing assignment
        List<string>? items = null;
        items ??= new List<string>();
        items.Add("item");
        
        // Also works with collection expressions
        List<int>? numbers = null;
        numbers ??= [];
        numbers.Add(42);
        
        // ❌ Old way - avoid this pattern
        List<string>? oldItems = null;
        if (oldItems == null)
        {
            oldItems = new List<string>();
        }
    }
    
    // ✅ Simplifying null checks with lazy initialization
    public string GetValue() => this.cachedValue ??= ExpensiveOperation();
    
    // ✅ With more complex initialization
    public Dictionary<string, object> GetCache() => 
        this.cache ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    
    // ✅ In property getters
    private ILogger? logger;
    private ILogger Logger => this.logger ??= CreateLogger();
    
    private string ExpensiveOperation()
    {
        // Simulate expensive operation
        Thread.Sleep(100);
        return "Computed Value";
    }
    
    private ILogger CreateLogger() => new ConsoleLogger();
}

// Stub for example
public interface ILogger { }
public class ConsoleLogger : ILogger { }
```

## **Null-Conditional Operators (C# 6+)**
```csharp
// ✅ Modern - Null-conditional access
string? result = user?.Profile?.Address?.City;
int count = items?.Count ?? 0;

// ✅ Null-conditional with method calls
user?.UpdateLastLogin();
collection?.Clear();

// ✅ Null-conditional with indexers
string? first = names?[0];
```

## **Index and Range Operators (C# 8)**
```csharp
// ✅ Modern - Index and range operators
string text = "Hello World";
char lastChar = text[^1];        // 'd'
char secondLast = text[^2];      // 'l'

string[] words = ["Hello", "World", "From", "C#"];
string[] lastTwo = words[^2..];   // ["From", "C#"]
string[] firstTwo = words[..2];   // ["Hello", "World"]
string[] middle = words[1..3];    // ["World", "From"]
```

## **Params Collections (C# 13)**
```csharp
// ✅ Modern - Params with any collection type
public void ProcessItems(params ReadOnlySpan<int> items)
{
    foreach (int item in items)
        Console.WriteLine(item);
}

public void ProcessNames(params IEnumerable<string> names)
{
    foreach (string name in names)
        Console.WriteLine(name);
}

// Usage
ProcessItems([1, 2, 3, 4]);
ProcessNames(["Alice", "Bob", "Charlie"]);
```

## **Semi-Auto Properties (C# 13)**
```csharp
// ✅ Modern - Semi-auto properties with field keyword
public class Person
{
    public string Name 
    { 
        get => field; 
        set => field = value?.Trim() ?? throw new ArgumentNullException();
    }
    
    public int Age 
    { 
        get => field; 
        set => field = value >= 0 ? value : throw new ArgumentException();
    }
}
```

## **UTF-8 String Literals (C# 11)**
```csharp
// ✅ Modern - UTF-8 string literals
ReadOnlySpan<byte> utf8Text = "Hello World"u8;
byte[] utf8Bytes = "Hello World"u8.ToArray();

// ✅ Useful for APIs expecting UTF-8
public void ProcessUtf8(ReadOnlySpan<byte> utf8Data) { }
ProcessUtf8("Some text"u8);
```

## **Implicit Index Access (C# 13)**
```csharp
// ✅ Modern - Implicit index in object initializers
var countdown = new TimerRemaining()
{
    buffer = 
    { 
        [^1] = 0, 
        [^2] = 1, 
        [^3] = 2, 
        [^4] = 3 
    }
};
```

## **Modern LINQ and Functional Idioms**
```csharp
// ✅ Modern - Method chaining and LINQ
var processedData = sourceData
    .Where(x => x.IsActive)
    .Select(x => new { x.Name, x.Value })
    .OrderBy(x => x.Name)
    .ToList();

// ✅ Using local functions
public IEnumerable<int> GetValidNumbers(IEnumerable<int> input)
{
    return input.Where(IsValid).Select(Transform);
    
    static bool IsValid(int n) => n > 0;
    static int Transform(int n) => n * 2;
}
```

## **String Interpolation Best Practices**
```csharp
// ✅ Modern - Interpolated strings with formatting
decimal price = 123.456m;
Console.WriteLine($"Price: {price:C}");

// ✅ With alignment
Console.WriteLine($"{"Name",-20} {"Price",10:C}");

// ✅ Multi-line interpolation
string message = $"""
    Dear {customer.Name},
    
    Your order #{order.Id} totaling {order.Total:C} 
    has been processed.
    
    Thank you!
    """;
```

## **Exception Handling Idioms**
```csharp
// ✅ Modern - Throw expressions
public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

// ✅ Pattern matching in catch
try
{
    // risky operation
}
catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
{
    // Handle specific exceptions
}
```

## **Async/Await Best Practices**
```csharp
// ✅ Modern - ValueTask for hot paths
public async ValueTask<string> GetCachedValueAsync(string key)
{
    if (_cache.TryGetValue(key, out var cached))
        return cached; // No allocation when cached
        
    return await FetchFromDatabaseAsync(key);
}

// ✅ ConfigureAwait(false) in libraries
public async Task<Data> GetDataAsync()
{
    var response = await httpClient.GetAsync(url).ConfigureAwait(false);
    return await response.Content.ReadFromJsonAsync<Data>().ConfigureAwait(false);
}
```

## **Immutable Collections Usage**
```csharp
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MyApp.Collections;

public class ImmutableCollectionExamples
{
    // ✅ Modern - Using appropriate immutable collection
    
    // For lookup tables (create once, read many)
    private static readonly FrozenDictionary<string, int> StatusCodes = new Dictionary<string, int>
    {
        ["OK"] = 200,
        ["Created"] = 201,
        ["BadRequest"] = 400,
        ["Unauthorized"] = 401,
        ["NotFound"] = 404,
        ["Error"] = 500
    }.ToFrozenDictionary();
    
    // For configuration that never changes
    private static readonly FrozenSet<string> AllowedFileExtensions = new HashSet<string>
    {
        ".txt", ".csv", ".json", ".xml"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    
    public void DemonstrateImmutableCollections()
    {
        // For frequent modifications
        ImmutableList<string> tags = ImmutableList<string>.Empty;
        tags = tags.Add("important").Add("urgent");
        
        // Better: use collection expression for initial values
        ImmutableList<string> initialTags = ["todo", "feature"];
        ImmutableList<string> updatedTags = initialTags.AddRange(["important", "urgent"]);
        
        // For small, read-heavy collections
        ImmutableArray<string> permissions = ["read", "write", "delete"];
        
        // Checking values
        bool canRead = permissions.Contains("read");
        int statusCode = StatusCodes.GetValueOrDefault("NotFound", 500);
    }
    
    // Real-world example: Building an immutable configuration
    public class ServerConfiguration
    {
        public FrozenDictionary<string, string> Settings { get; }
        public FrozenSet<int> AllowedPorts { get; }
        public ImmutableArray<string> Endpoints { get; }
        
        public ServerConfiguration(
            IDictionary<string, string> settings,
            IEnumerable<int> ports,
            IEnumerable<string> endpoints)
        {
            this.Settings = settings.ToFrozenDictionary();
            this.AllowedPorts = ports.ToFrozenSet();
            this.Endpoints = [..endpoints];
        }
        
        public bool IsPortAllowed(int port) => this.AllowedPorts.Contains(port);
        public string GetSetting(string key) => this.Settings.GetValueOrDefault(key, string.Empty);
    }
}
```

## **Modern Validation Patterns**
```csharp
// ✅ Modern - Using ArgumentNullException.ThrowIfNull (C# 11)
public void ProcessData(string data)
{
    ArgumentNullException.ThrowIfNull(data);
    // Process data
}

// ✅ Guard clauses with throw expressions
public class User(string name, string email)
{
    public string Name { get; } = !string.IsNullOrWhiteSpace(name) 
        ? name 
        : throw new ArgumentException("Name cannot be empty");
        
    public string Email { get; } = IsValidEmail(email) 
        ? email 
        : throw new ArgumentException("Invalid email format");
}
```



### Naming Conventions

Follow .NET Design Guidelines for naming. For private members, use camelCase without an underscore prefix. Ensure private members are called with `this.` prefix to avoid confusion with local variables.
Avoid the use of `var`, prefer explicit types for clarity, especially in public APIs. Use `var` only when the type is obvious from the right-hand side.

### Target-Typed new() Expressions

Prefer target-typed `new()` expressions (C# 9.0+) for cleaner, more readable code:

```csharp
namespace MyApp.Services;

// ✅ GOOD: Target-typed new with explicit type on left
public class PaymentExample
{
    public void ProcessPayment()
    {
        Payment payment = new()
        {
            Amount = 100.00m,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };
        
        List<Order> orders = new();
        Dictionary<string, Product> products = new();
        
        // For collections, prefer collection expressions where applicable
        ImmutableList<string> tags = ["urgent", "payment"];
    }
    
    // ❌ AVOID: Using var with new
    private void BadExample()
    {
        // Don't do this - conflicts with avoiding var
        var payment = new Payment();
        
        // Also avoid redundant type specification
        Payment anotherPayment = new Payment();
    }
    
    // ✅ GOOD: Return statements with target-typed new
    public PaymentResult ProcessRefund(decimal amount)
    {
        if (amount <= 0)
        {
            return new()
            {
                Success = false,
                Message = "Amount must be positive"
            };
        }
        
        // Process refund...
        return new()
        {
            Success = true,
            Message = "Refund processed successfully",
            TransactionId = Guid.NewGuid().ToString()
        };
    }
}

// ✅ GOOD: In field/property initializers
public class OrderService
{
    private readonly List<Order> orders = new();
    private readonly Dictionary<string, decimal> taxRates = new()
    {
        ["US"] = 0.08m,
        ["CA"] = 0.13m,
        ["UK"] = 0.20m
    };
    
    // For immutable collections, use collection expressions
    private readonly ImmutableArray<string> supportedCurrencies = ["USD", "CAD", "GBP", "EUR"];
    
    // Primary constructor with readonly fields
    private readonly ILogger<OrderService> logger;
    
    public OrderService(ILogger<OrderService> logger)
    {
        this.logger = logger;
    }
}
```

This approach:
- Reduces redundancy
- Makes refactoring easier (change type in one place)
- Improves readability by focusing on the variable's purpose
- Aligns with modern C# idioms

### Solution Organization

```
\CONTRACTOPS.CLI
|   .dockerignore
|   .gitignore
|   README.md
\---Solutions
    |   .dockerignore
    |   .editorconfig
    |   ContractOps.Cli.sln
    |   stylecop.json
    |   StyleCop.ruleset
    +---ContractOps.Abstractions
    |   |   ContractOps.Abstractions.csproj
    |   |   NuGet.Readme.md
    |   +---ContractOps
    |   |   \---Abstractions
    |   |       |   WellKnown.cs
    |   |       +---Parsers
    |   |       |       YamlParser{T}.cs
    |   |       \---Primitives
    |   |               ContentBlock.cs
    +---ContractOps.Cli
    |   |   ContractOps.Cli.csproj
    |   |   Dockerfile
    |   |   Dockerfile.multi-stage
    |   |   NuGet.Readme.md
    |   +---ContractOps
    |   |   \---Cli
    |   |       |   Program.cs
    |   |       +---Commands
    |   |       |       GenerateCommand.cs
    |   |       \---Infrastructure
    |   |           |   ServiceCollectionExtensions.cs
    |   |           \---Injection
    |   |                   TypeRegistrar.cs
    |   |                   TypeResolver.cs
    |   |
    |   \---Properties
    |           launchSettings.json
    \---ContractOps.Specs
        |   ContractOps.Specs.csproj
        |   specflow.json
```

### Functional Programming Patterns

#### Immutable Data Structures

```csharp
using System.Collections.Immutable;

namespace MyApp.Domain;

// Using records with required properties for immutable domain models
public record Address
{
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string PostalCode { get; init; }
    public string Country { get; init; } = "USA";
    
    // Validation in constructor
    public Address()
    {
        // Record constructors run after init, so validation happens in methods
    }
    
    public static Address Create(string street, string city, string state, string postalCode, string? country = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(street);
        ArgumentException.ThrowIfNullOrEmpty(city);
        ArgumentException.ThrowIfNullOrEmpty(state);
        ArgumentException.ThrowIfNullOrEmpty(postalCode);
        
        return new Address
        {
            Street = street,
            City = city,
            State = state,
            PostalCode = postalCode,
            Country = country ?? "USA"
        };
    }
}

// Using primary constructor for dependencies
public record Customer
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public ImmutableList<Address> Addresses { get; init; } = [];
    public CustomerStatus Status { get; init; } = CustomerStatus.Active;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    // Business logic methods that return new instances
    public Customer UpdateEmail(string newEmail)
    {
        ArgumentException.ThrowIfNullOrEmpty(newEmail);
        
        if (!IsValidEmail(newEmail))
            throw new ArgumentException($"Invalid email format: {newEmail}", nameof(newEmail));
            
        return this with { Email = newEmail };
    }
    
    public Customer AddAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        
        const int MaxAddresses = 5;
        if (this.Addresses.Count >= MaxAddresses)
            throw new InvalidOperationException($"Customer cannot have more than {MaxAddresses} addresses");
            
        return this with { Addresses = this.Addresses.Add(address) };
    }
    
    public Customer RemoveAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        
        ImmutableList<Address> newAddresses = this.Addresses.Remove(address);
        if (newAddresses.Count == this.Addresses.Count)
            throw new InvalidOperationException("Address not found");
            
        return this with { Addresses = newAddresses };
    }
    
    public Customer Activate() => 
        this.Status == CustomerStatus.Active 
            ? this 
            : this with { Status = CustomerStatus.Active };
    
    public Customer Deactivate() =>
        this.Status == CustomerStatus.Inactive
            ? this
            : this with { Status = CustomerStatus.Inactive };
    
    private static bool IsValidEmail(string email) =>
        EmailRegex.IsMatch(email);
}

public enum CustomerStatus
{
    Active,
    Inactive,
    Suspended
}

// Test showing immutability and business logic
[TestMethod]
public void Customer_UpdateEmail_WithValidEmail_CreatesNewInstanceWithUpdatedEmail()
{
    // Arrange
    Customer original = new()
    {
        Id = Guid.NewGuid(),
        Name = "John Doe",
        Email = "john@example.com"
    };
    
    // Act
    Customer updated = original.UpdateEmail("john.doe@company.com");
    
    // Assert
    updated.ShouldNotBeSameAs(original);
    updated.Email.ShouldBe("john.doe@company.com");
    original.Email.ShouldBe("john@example.com");
    updated.Id.ShouldBe(original.Id);
    updated.Name.ShouldBe(original.Name);
}

[TestMethod]
public void Customer_UpdateEmail_WithInvalidEmail_ThrowsException()
{
    // Arrange
    Customer customer = new()
    {
        Id = Guid.NewGuid(),
        Name = "John Doe",
        Email = "john@example.com"
    };
    
    // Act & Assert
    ArgumentException exception = Should.Throw<ArgumentException>(() => customer.UpdateEmail("invalid-email"));
    
    exception.Message.ShouldBe("Invalid email format");
}
```

#### Pure Functions and Composition

```csharp
namespace MyApp.Domain.Pricing;

// Pure functions for business logic with proper types
public record OrderItem
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    
    public decimal LineTotal => Quantity * UnitPrice;
    
    // Factory method with validation
    public static OrderItem Create(string productId, int quantity, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrEmpty(productId);
        
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive");
            
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative");
            
        return new OrderItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}

public record PricingContext
{
    public required decimal DiscountPercent { get; init; }
    public required decimal TaxRate { get; init; }
    public decimal MinimumOrderAmount { get; init; } = 0m;
    public decimal FreeShippingThreshold { get; init; } = decimal.MaxValue;
    public decimal StandardShippingCost { get; init; } = 9.99m;
    
    // Validation
    public PricingContext()
    {
        // Record validation happens after init
    }
    
    public static PricingContext Create(
        decimal discountPercent,
        decimal taxRate,
        decimal? minimumOrderAmount = null,
        decimal? freeShippingThreshold = null)
    {
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Must be between 0 and 100");
            
        if (taxRate < 0 || taxRate > 100)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Must be between 0 and 100");
            
        return new PricingContext
        {
            DiscountPercent = discountPercent,
            TaxRate = taxRate,
            MinimumOrderAmount = minimumOrderAmount ?? 0m,
            FreeShippingThreshold = freeShippingThreshold ?? decimal.MaxValue
        };
    }
}

public static class PricingCalculator
{
    public static decimal CalculateSubtotal(IEnumerable<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Sum(item => item.LineTotal);
    }
    
    public static decimal ApplyDiscount(decimal subtotal, decimal discountPercent)
    {
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Must be between 0 and 100");
            
        return subtotal * (1 - discountPercent / 100);
    }
    
    public static decimal CalculateTax(decimal amount, decimal taxRate)
    {
        if (taxRate < 0)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Cannot be negative");
            
        return Math.Round(amount * (taxRate / 100), 2, MidpointRounding.AwayFromZero);
    }
    
    public static decimal CalculateShipping(decimal subtotal, decimal threshold, decimal standardCost)
    {
        if (threshold < 0)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Cannot be negative");
            
        if (standardCost < 0)
            throw new ArgumentOutOfRangeException(nameof(standardCost), "Cannot be negative");
            
        return subtotal >= threshold ? 0m : standardCost;
    }
    
    public static OrderTotals CalculateOrderTotals(IEnumerable<OrderItem> items, PricingContext context)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(context);
        
        ImmutableArray<OrderItem> itemArray = [..items];
        if (itemArray.IsEmpty)
            throw new ArgumentException("Order must contain at least one item", nameof(items));
        
        decimal subtotal = CalculateSubtotal(itemArray);
        
        // Apply minimum order check
        if (subtotal < context.MinimumOrderAmount)
            throw new InvalidOperationException($"Order minimum of {context.MinimumOrderAmount:C} not met. Current subtotal: {subtotal:C}");
        
        decimal discountAmount = subtotal * (context.DiscountPercent / 100);
        decimal discountedSubtotal = subtotal - discountAmount;
        decimal tax = CalculateTax(discountedSubtotal, context.TaxRate);
        decimal shipping = CalculateShipping(
            subtotal, 
            context.FreeShippingThreshold, 
            context.StandardShippingCost);
        
        return new OrderTotals
        {
            Items = itemArray,
            Subtotal = subtotal,
            DiscountPercent = context.DiscountPercent,
            DiscountAmount = discountAmount,
            TaxRate = context.TaxRate,
            TaxAmount = tax,
            ShippingAmount = shipping,
            Total = discountedSubtotal + tax + shipping
        };
    }
    
    // Compose smaller functions for specific scenarios
    public static decimal CalculateFinalPrice(
        decimal basePrice,
        decimal discountPercent,
        decimal taxRate)
    {
        decimal discounted = ApplyDiscount(basePrice, discountPercent);
        decimal tax = CalculateTax(discounted, taxRate);
        return discounted + tax;
    }
}

public record OrderTotals
{
    public required ImmutableArray<OrderItem> Items { get; init; }
    public required decimal Subtotal { get; init; }
    public required decimal DiscountPercent { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required decimal TaxRate { get; init; }
    public required decimal TaxAmount { get; init; }
    public required decimal ShippingAmount { get; init; }
    public required decimal Total { get; init; }
}

// Tests demonstrating pure function behavior and composition
[TestMethod]
public void CalculateOrderTotals_WithDiscountTaxAndShipping_ReturnsCorrectBreakdown()
{
    // Arrange
    OrderItem[] items = new[]
    {
        new() { ProductId = "PROD1", Quantity = 2, UnitPrice = 50.00m },
        new() { ProductId = "PROD2", Quantity = 1, UnitPrice = 30.00m }
    };
    
    PricingContext context = new()
    {
        DiscountPercent = 10m,
        TaxRate = 8m,
        FreeShippingThreshold = 150m,
        StandardShippingCost = 9.99m
    };
    
    // Act
    OrderTotals totals = PricingCalculator.CalculateOrderTotals(items, context);
    
    // Assert
    totals.Subtotal.ShouldBe(130.00m);
    totals.DiscountAmount.ShouldBe(13.00m);
    totals.TaxAmount.ShouldBe(9.36m);
    totals.ShippingAmount.ShouldBe(9.99m); // Below free shipping threshold
    totals.Total.ShouldBe(136.35m); // 117 + 9.36 + 9.99
}

[TestMethod]
public void CalculateOrderTotals_AboveFreeShippingThreshold_WaivesShipping()
{
    // Arrange
    OrderItem[] items = new[]
    {
        new() { ProductId = "PROD1", Quantity = 3, UnitPrice = 60.00m }
    };
    
    PricingContext context = new()
    {
        DiscountPercent = 0m,
        TaxRate = 8m,
        FreeShippingThreshold = 150m,
        StandardShippingCost = 9.99m
    };
    
    // Act
    var totals = PricingCalculator.CalculateOrderTotals(items, context);
    
    // Assert
    totals.Subtotal.ShouldBe(180.00m);
    totals.ShippingAmount.ShouldBe(0m); // Free shipping applied
    totals.Total.ShouldBe(194.40m); // 180 + 14.40 tax
}
```