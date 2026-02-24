# Dependency Injection

**Namespace:** `Sencilla.Core`
**Source:** `libs/core/Injection/`

Sencilla provides **attribute-based, convention-driven DI** on top of the standard `Microsoft.Extensions.DependencyInjection` container. You declare intent with attributes; the framework wires everything up on startup.

---

## Registration Attributes

### `[Implement]` — Scoped (per-request) lifetime

The most common attribute. Registers the class as a **scoped** service for all interfaces it implements.

```csharp
[Implement]
public class ProductRepository : ReadRepository<Product, AppDbContext>,
    ICreateRepository<Product, int>
{
    // ...
}
```

This registers:
- `IReadRepository<Product, int>` → `ProductRepository` (scoped)
- `ICreateRepository<Product, int>` → `ProductRepository` (scoped)

---

### `[SingletonLifetime]` — Singleton lifetime

Register the class as a **singleton** (one instance for the app lifetime).

```csharp
[SingletonLifetime]
public class CacheService : ICacheService
{
    // ...
}
```

Use for stateless services, caches, and services that are expensive to construct.

---

### `[PerRequestLifetime]` — Transient lifetime

Register the class as **transient** (new instance per injection).

```csharp
[PerRequestLifetime]
public class EmailBuilder : IEmailBuilder
{
    // ...
}
```

Use when the service holds mutable state that must not be shared.

---

### `[DisableInjection]`

Exclude a class from auto-discovery registration entirely.

```csharp
[DisableInjection]
public class InternalHelper : IHelper
{
    // Will NOT be registered
}
```

---

## Assembly Scanning — `[AutoDiscovery]`

Auto-discovery scans assemblies marked with `[AutoDiscovery]`. Sencilla's own assemblies are already marked. For your application code:

```csharp
// Mark your assembly entry point
[AutoDiscovery]
public class MyAppAssemblyMarker { }
```

Or, more commonly, pass your assembly directly to `AddSencilla`:

```csharp
builder.Services.AddSencilla(typeof(Program).Assembly);
```

This overload marks your assembly for scanning automatically.

---

## `AddSencilla` — The Entry Point

```csharp
// Program.cs
builder.Services.AddSencilla(typeof(Program).Assembly);
```

Internally this:
1. Marks the provided assembly with `[AutoDiscovery]`
2. Scans all `[AutoDiscovery]`-marked assemblies for `[Implement]`, `[Singleton]`, `[PerRequest]` classes
3. Registers discovered services with the appropriate lifetime
4. Registers command dispatchers, event dispatchers, and middleware
5. Registers `ISencillaApp` service provider

---

## `ITypeRegistrator` — Custom Registration Logic

For cases where attributes are not enough, implement `ITypeRegistrator`:

```csharp
public interface ITypeRegistrator
{
    void Register(IServiceCollection services, IEnumerable<Type> discoveredTypes);
}
```

```csharp
[Implement]
public class MyCustomRegistrator : ITypeRegistrator
{
    public void Register(IServiceCollection services, IEnumerable<Type> discoveredTypes)
    {
        // Manual registrations that depend on runtime conditions
        services.AddSingleton<ISpecialService, SpecialServiceImpl>();
    }
}
```

---

## Lifetimes Reference

| Attribute | Lifetime | Instance scope |
| --------- | -------- | -------------- |
| `[Implement]` | Scoped | Per HTTP request (or DI scope) |
| `[SingletonLifetime]` | Singleton | App lifetime |
| `[PerRequestLifetime]` | Transient | Per injection point |

---

## Registration Rules

1. **Interface mapping** — a class is registered for every interface it directly implements (not base class interfaces, unless the class also lists them).

2. **Multiple interfaces** — one concrete class can satisfy many interfaces.

   ```csharp
   [Implement]
   public class UserRepository :
       ReadRepository<User, AppDbContext>,
       ICreateRepository<User, int>,
       IUpdateRepository<User, int>
   { ... }
   // Registers: IReadRepository<User,int>, ICreateRepository<User,int>, IUpdateRepository<User,int>
   ```

3. **Inheritance** — sub-classes inherit their parent's `[Implement]` attribute if the parent has one, but it's cleaner to decorate the concrete class.

4. **Override** — you can always register manually with `services.AddScoped<>()` after `AddSencilla()`; manual registrations take precedence.

---

## Practical Patterns

### Repository with a custom method

```csharp
// Custom interface
public interface IProductRepository : ICreateRepository<Product, int>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
}

// Implementation
[Implement]
public class ProductRepository :
    ReadRepository<Product, AppDbContext>,
    IProductRepository
{
    public ProductRepository(RepositoryDependency<AppDbContext> dep) : base(dep) { }

    public Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        => Task.FromResult(Query.Where(p => p.CategoryId == categoryId).AsEnumerable());
}

// Consumer injects the specific interface
public class ProductService(IProductRepository repo) { ... }
```

### Service with singleton cache

```csharp
[SingletonLifetime]
public class CountryCache : ICountryCache
{
    private readonly IReadRepository<Country, string> _repo;  // watch for captive dependency!
    private Dictionary<string, Country> _cache = new();

    // Note: avoid injecting scoped services into singletons.
    // Use IServiceScopeFactory for that pattern.
    public CountryCache(IServiceScopeFactory factory)
    {
        // ...
    }
}
```

> **Captive dependency warning:** Never inject a scoped service (`[Implement]`) into a singleton (`[SingletonLifetime]`). Use `IServiceScopeFactory` to create a scope when needed.

---

## StarterKit

`StarterKit` is the internal class that orchestrates DI setup. It is called by `AddSencilla`. You rarely need to interact with it directly, but it exposes the registration pipeline if you need to extend it.

---

## See Also

- [Getting Started](../getting-started.md) — `AddSencilla` in context
- [Repository Interfaces](repositories.md) — what `[Implement]` does for repositories
- [Commands & Events](commands-events.md) — handler auto-registration
