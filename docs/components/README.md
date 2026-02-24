# Components

> **Status:** Documentation in progress. Module structure and quick-start guides are included. Detailed API references are planned.

Sencilla Components are **pre-built domain modules** for concerns that appear in almost every web application. Each component is an independent NuGet package; install only what you need.

---

## Available Components

| Package | Domain | Key Capability |
| ------- | ------ | -------------- |
| `Sencilla.Component.Users` | Identity | User entity, profile, user repository |
| `Sencilla.Component.Auth` | Identity | JWT, OAuth 2.0, authentication flows |
| `Sencilla.Component.Security` | Identity | Roles, permissions, policy-based authorization |
| `Sencilla.Component.Config` | Infrastructure | Runtime key-value configuration management |
| `Sencilla.Component.I18n` | Localization | Translation strings, locale selection |
| `Sencilla.Component.Geography` | Domain Data | Countries, regions, cities |
| `Sencilla.Component.Validation` | Cross-cutting | Validation rule pipelines |

---

## Users — `Sencilla.Component.Users`

```bash
dotnet add package Sencilla.Component.Users
```

Provides a base `User` entity implementing `IEntity<Guid>`, `IEntityCreateable`, `IEntityUpdateable`, `IEntityRemoveable`, along with a user repository and basic profile management.

```csharp
// Extend the base user
public class AppUser : User
{
    public string Department { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

// Inject and use
public class ProfileService(IReadRepository<AppUser, Guid> users)
{
    public Task<AppUser?> GetProfileAsync(Guid userId) =>
        users.GetById(userId);
}
```

---

## Security — `Sencilla.Component.Security`

```bash
dotnet add package Sencilla.Component.Security
```

Provides roles, permissions, and authorization helpers built on top of ASP.NET Core's policy infrastructure.

```csharp
// Check permission in a handler
public class DeleteProductHandler(IAuthorizationService auth, ...)
{
    public async Task HandleAsync(DeleteProductCommand cmd, CancellationToken token)
    {
        await auth.AuthorizeOrThrowAsync(currentUser, "products.delete");
        // ...
    }
}
```

---

## Config — `Sencilla.Component.Config`

```bash
dotnet add package Sencilla.Component.Config
```

Stores application configuration as key-value pairs in a database (editable at runtime without redeployment).

```csharp
// Get a typed config value
var maxUpload = await configService.GetAsync<int>("Storage.MaxFileSizeMB");

// Set a value
await configService.SetAsync("Notifications.EmailEnabled", true);
```

---

## I18n — `Sencilla.Component.I18n`

```bash
dotnet add package Sencilla.Component.I18n
```

Database-backed translation strings with locale selection.

```csharp
// Resolve translation
var label = await i18n.TranslateAsync("products.title", locale: "fr-FR");

// Fallback to default locale if not found
var label = await i18n.TranslateAsync("products.title", locale: "fr-FR", fallback: "en-US");
```

---

## Geography — `Sencilla.Component.Geography`

```bash
dotnet add package Sencilla.Component.Geography
```

Pre-seeded country, region, and city data with ISO codes.

```csharp
var countries = await countryRepo.GetAll();
var regions = await regionRepo.GetAll(new Filter()
    .AddProperty("CountryCode", FilterPropertyType.Equal, "US"));
```

---

## Dependencies Between Components

```text
Sencilla.Component.Security
    └── depends on → Sencilla.Component.Users
                     Sencilla.Web

Sencilla.Component.Files
    └── depends on → Sencilla.Component.Config
                     Sencilla.Web

All components
    └── depend on → Sencilla.Core
```

---

## Coming Soon

- Auth component: JWT setup guide, refresh token flow
- Security: custom permission definitions, policy builders
- Validation: rule pipeline, integration with FluentValidation
- Full API reference for each component

---

## See Also

- [Files](../files/README.md) — file storage component
- [Architecture](../architecture.md) — component dependency graph
- [Dependency Injection](../core/dependency-injection.md) — component registration
