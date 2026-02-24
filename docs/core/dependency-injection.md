# Dependency Injection

**Namespace:** `Sencilla.Core`
**Source:** `libs/core/Injection/`

Sencilla provides **attribute-based, convention-driven DI** on top of the standard `Microsoft.Extensions.DependencyInjection` container. You declare intent with attributes; the framework wires everything up on startup.

---

## Registration Attributes

### `[Implement]` — Register a custom service

Registers the class for all interfaces it explicitly declares. Use it for your own services — **not** for repositories (those are auto-registered from entity interfaces).

```csharp
[Implement]
public class NotificationService : INotificationService
{
    // Auto-registered as INotificationService
}
```

> **Note:** Standard repositories (`IReadRepository<T>`, `ICreateRepository<T>`, etc.) are auto-registered when you define an entity with the matching lifecycle interfaces. You do **not** need `[Implement]` for them. See [Auto-Registration](repositories.md#auto-registration).

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

Or mark your assembly with `[assembly: AutoDiscovery]` in any file (e.g. `AssemblyInfo.cs`):

```csharp
[assembly: AutoDiscovery]
```

---

## `AddSencilla` — The Entry Point

```csharp
// Program.cs
builder.Services.AddSencilla(builder.Configuration);
```

Internally this:

1. Scans all `[AutoDiscovery]`-marked assemblies for types
2. Discovers all `ITypeRegistrator` implementations and invokes them for every type
3. Registers `[Implement]`, `[SingletonLifetime]`, `[PerRequestLifetime]` services
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
   public class OrderService : IOrderService, IOrderValidator
   { ... }
   // Registers both IOrderService and IOrderValidator → OrderService
   ```

3. **Inheritance** — sub-classes inherit their parent's `[Implement]` attribute if the parent has one, but it's cleaner to decorate the concrete class.

4. **Override** — you can always register manually with `services.AddScoped<>()` after `AddSencilla()`; manual registrations take precedence.

---

## Practical Patterns

### Service using auto-registered repositories

Standard repositories are auto-registered from entity interfaces — just inject them:

```csharp
// Product entity — IEntityCreateable + IEntityUpdateable → auto-registers create + update repos
public class Product : IEntity<int>, IEntityCreateable, IEntityUpdateable { ... }

// Service — inject repository interfaces directly
[Implement]
public class ProductService : IProductService
{
    private readonly IReadRepository<Product> _reader;
    private readonly ICreateRepository<Product> _creator;

    public ProductService(IReadRepository<Product> reader, ICreateRepository<Product> creator)
    {
        _reader = reader;
        _creator = creator;
    }

    public Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
    {
        var filter = new Filter();
        filter.AddProperty("CategoryId", typeof(int), categoryId);
        return _reader.GetAll(filter);
    }
}
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
