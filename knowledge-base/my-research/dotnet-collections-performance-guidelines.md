## .NET / C# Immutable Collections Performance Guide

### Collection Selection Matrix

**FrozenSet<T> / FrozenDictionary<TKey,TValue>** (.NET 8+)
- **Use when:** Create once, read frequently (config, lookups, caches)
- **Performance:** Fastest reads (17-50% faster than HashSet/Dictionary), expensive creation
- **Trade-off:** High creation cost for optimal read performance

**ImmutableArray<T>**
- **Use when:** Small collections (<16 elements), rare modifications, need O(1) access
- **Performance:** Fastest iteration, O(1) element access, O(n) modifications
- **Trade-off:** Full array copy on any change

**ImmutableList<T>**
- **Use when:** Frequent modifications on large collections, need balanced performance
- **Performance:** O(log n) access/modifications, efficient memory sharing via AVL trees
- **Trade-off:** Slower element access but efficient updates

**Records with Immutable Properties**
- **Use when:** Data transfer objects, value objects, simple immutable data
- **Features:** `init` keywords, `required` modifier, `with` expressions
- **Best for:** Business domain objects, API models

### Performance Best Practices

1. **Creation:** Use `Create()` methods or builders for optimal performance
2. **Iteration:** Prefer `foreach` over indexed loops for tree-based collections
3. **Avoid:** ReadOnlyCollection<T> for true immutability (it's just a wrapper)
4. **Memory:** ImmutableArray for lowest overhead, ImmutableList for sharing

### Quick Decision Tree
- Need maximum read performance + rare changes? → **FrozenSet/Dictionary**
- Small collection + performance critical? → **ImmutableArray**
- Large collection + frequent updates? → **ImmutableList**
- Simple data objects? → **Records with init/required**

### Code Examples
```csharp
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace MyApp.Collections;

// Frozen collections for configuration and lookup tables
public class ProductCatalog(IEnumerable<Product> products)
{
    private readonly FrozenDictionary<string, Product> frozenProducts = products.ToFrozenDictionary(p => p.Sku);
    
    public Product? FindBySku(string sku) =>
        this.frozenProducts.GetValueOrDefault(sku);
    
    public int Count => this.frozenProducts.Count;
}

// ImmutableArray for small, frequently accessed collections  
public static class OrderStatus
{
    public static readonly ImmutableArray<string> ValidStatuses = ["Pending", "Processing", "Shipped", "Delivered"];
    
    public static bool IsValid(string status) =>
        ValidStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
}

// ImmutableList for collections that change frequently
public sealed class ShoppingCart
{
    private readonly ImmutableList<CartItem> items;
    
    public ShoppingCart() : this(ImmutableList<CartItem>.Empty) { }
    
    private ShoppingCart(ImmutableList<CartItem> items)
    {
        this.items = items;
    }
    
    public IReadOnlyList<CartItem> Items => this.items;
    public int ItemCount => this.items.Count;
    public bool IsEmpty => this.items.IsEmpty;
    
    public ShoppingCart AddItem(CartItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new(this.items.Add(item));
    }
    
    public ShoppingCart RemoveItem(string productId)
    {
        ArgumentException.ThrowIfNullOrEmpty(productId);
        return new(this.items.RemoveAll(i => i.ProductId == productId));
    }
    
    public ShoppingCart Clear() => new(ImmutableList<CartItem>.Empty);
}

// Records with required properties for domain models
public record Product
{
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required ProductCategory Category { get; init; }
    
    // Business logic methods can still be added
    public decimal CalculateTax(decimal taxRate)
    {
        if (taxRate < 0 || taxRate > 1)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate must be between 0 and 1");
            
        return Price * taxRate;
    }
}

public record CartItem
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    
    public decimal LineTotal => Quantity * UnitPrice;
}

// Example usage with target-typed new and proper patterns
public class OrderExample
{
    private readonly FrozenDictionary<ProductCategory, decimal> taxRates = new Dictionary<ProductCategory, decimal>
    {
        [ProductCategory.Electronics] = 0.08m,
        [ProductCategory.Food] = 0.05m,
        [ProductCategory.Clothing] = 0.07m
    }.ToFrozenDictionary();
    
    public ShoppingCart CreateSampleCart()
    {
        // Using target-typed new for better readability
        Product product = new()
        {
            Sku = "PROD-001",
            Name = "Premium Widget",
            Price = 99.99m,
            Category = ProductCategory.Electronics
        };
        
        ShoppingCart cart = new();
        CartItem item = new()
        {
            ProductId = product.Sku,
            Quantity = 2,
            UnitPrice = product.Price
        };
        
        return cart.AddItem(item);
    }
    
    public decimal CalculateTotalWithTax(ShoppingCart cart)
    {
        decimal subtotal = cart.Items.Sum(item => item.LineTotal);
        // In real code, you'd look up tax rate based on actual products
        decimal taxRate = this.taxRates.GetValueOrDefault(ProductCategory.Electronics, 0.08m);
        return subtotal * (1 + taxRate);
    }
}