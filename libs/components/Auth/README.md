# Sencilla.Component.Users.Auth

Authentication component for the [Sencilla Framework](https://github.com/vitalykovalgit/Sencilla). It issues **JWT access tokens**, manages **rotating refresh tokens**, and handles **registration / login** on top of the `Sencilla.Component.Users` identity model.

- **Namespace:** `Sencilla.Component.Users.Auth`
- **Package:** `Sencilla.Component.Users.Auth`
- **Depends on:** `Sencilla.Component.Users`, `Sencilla.Core`, `System.IdentityModel.Tokens.Jwt`

---

## Table of Contents

1. [Installation](#installation)
2. [What the component provides](#what-the-component-provides)
3. [Quick start](#quick-start)
   - [1. Configure `AuthOptions`](#1-configure-authoptions)
   - [2. Register Sencilla](#2-register-sencilla)
   - [3. Validate the issued JWTs](#3-validate-the-issued-jwts-jwt-bearer)
   - [4. Create the database tables](#4-create-the-database-tables)
4. [Using `IAuthService`](#using-iauthservice)
   - [Register](#register)
   - [Login](#login)
   - [Refresh](#refresh)
   - [Wiring up endpoints](#wiring-up-endpoints)
5. [API reference](#api-reference)
   - [`IAuthService`](#iauthservice)
   - [`AuthOptions`](#authoptions)
   - [Request / response DTOs](#request--response-dtos)
   - [`UserRefreshToken` entity](#userrefreshtoken-entity)
   - [`PasswordHashExtensions`](#passwordhashextensions)
6. [How it works](#how-it-works)
7. [Behavioral notes & limitations](#behavioral-notes--limitations)
8. [Security guidance](#security-guidance)
9. [See also](#see-also)

---

## Installation

```bash
dotnet add package Sencilla.Component.Users.Auth
```

This package transitively pulls in `Sencilla.Component.Users` and `Sencilla.Core`. The assembly is marked with `[assembly: AutoDiscovery]`, so once it is referenced, `AddSencilla` discovers and registers everything automatically — no manual `AddScoped<IAuthService, AuthService>()` is required.

> **Tip — force the assembly to load.** If nothing in your app references a type from this package directly, the assembly may not be loaded at startup and auto-discovery will skip it. Call the no-op marker once in `Program.cs` to guarantee the reference:
>
> ```csharp
> builder.Services.AddSencillaUsersRegistration();
> ```

---

## What the component provides

| Type | Kind | Purpose |
| ---- | ---- | ------- |
| `IAuthService` / `AuthService` | Service | Registration, login, and refresh-token rotation. Auto-registered as **transient**. |
| `AuthOptions` | Options | JWT signing key, issuer, audience, access-token lifetime. |
| `RegisterRequest` | DTO | Input for `RegisterAsync`. |
| `LoginRequest` | DTO | Input for `LoginAsync`. |
| `TokenResponse` | DTO | Output of all three operations (`AccessToken`, `RefreshToken`, `ExpiresAt`). |
| `UserRefreshToken` | Entity | Persisted refresh token (`sec.UserRefreshToken`). |
| `UserRefreshTokenFilter` | Filter | Query refresh tokens by token value. |
| `PasswordHashExtensions` | Extensions | `HashPassword()` / `VerifyPassword()` helpers (ASP.NET Core Identity `PasswordHasher`). |

The component **issues and stores** tokens. It does **not** configure the ASP.NET Core authentication middleware that *validates* incoming tokens — you wire that up yourself (see [step 3](#3-validate-the-issued-jwts-jwt-bearer)) so the access/validation sides share the same key, issuer, and audience.

---

## Quick start

### 1. Configure `AuthOptions`

`AuthService` is constructed with `IOptions<AuthOptions>`, so you must bind the options. Add a section to `appsettings.json`:

```json
{
  "Auth": {
    "SecretKey": "REPLACE-WITH-A-LONG-RANDOM-SECRET-AT-LEAST-32-CHARS",
    "Issuer": "https://your-app.example.com",
    "Audience": "https://your-app.example.com",
    "JwtExpiresMinutes": 60
  }
}
```

Bind it in `Program.cs`:

```csharp
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
```

> `SecretKey` is used with `HmacSha256`, which requires a key of **at least 256 bits (32 bytes)**. Use a long, random value and keep it out of source control (user-secrets, environment variables, or a secrets vault).

### 2. Register Sencilla

```csharp
using Sencilla.Core;

var builder = WebApplication.CreateBuilder(args);

// Bind JWT options
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));

// Core DI + auto-discovery (registers IAuthService and all repositories)
builder.Services.AddSencilla(builder.Configuration);

// EF Core repositories — discovers User, UserPassword, UserRefreshToken, etc.
builder.Services.AddSencillaRepositoryForEF(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Ensure the Auth assembly is referenced so auto-discovery sees it
builder.Services.AddSencillaUsersRegistration();
```

`AuthService` depends on three repositories that the EF integration auto-registers from the entity definitions:

- `ICreateRepository<User>`
- `IUpdateRepository<UserPassword>`
- `ICreateRepository<UserRefreshToken>`

### 3. Validate the issued JWTs (JWT Bearer)

To let the tokens this component issues protect your endpoints, configure the bearer middleware with the **same** secret/issuer/audience:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var auth = builder.Configuration.GetSection("Auth").Get<AuthOptions>()!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = auth.Issuer,
            ValidAudience = auth.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(auth.SecretKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

> Requires the `Microsoft.AspNetCore.Authentication.JwtBearer` package in the host application.

### 4. Create the database tables

The component persists refresh tokens to `sec.UserRefreshToken`. The DDL ships with the package at
[`Database/Tables/UserRefreshToken.sql`](Database/Tables/UserRefreshToken.sql):

```sql
CREATE TABLE [sec].[UserRefreshToken]
(
    [Id]               INT IDENTITY NOT NULL,
    [UserId]           INT NOT NULL,
    [Token]            NVARCHAR(2000) NOT NULL,
    [ExpiresAt]        DATETIME2(7) NOT NULL,
    [CreatedAt]        DATETIME2(7) NOT NULL,
    [CreatedByIp]      NVARCHAR(45)  NULL,
    [RevokedAt]        DATETIME2(7)  NULL,
    [RevokedByIp]      NVARCHAR(45)  NULL,
    [ReplacedByToken]  NVARCHAR(2000) NULL,

    CONSTRAINT [PK_UserRefreshToken] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserRefreshToken_UserId] FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id])
)
```

You also need the `sec.User` and the user-password storage from `Sencilla.Component.Users`.

---

## Using `IAuthService`

Inject `IAuthService` anywhere in the DI container — a controller, a minimal-API handler, or another service.

```csharp
public class AccountService(IAuthService auth)
{
    public Task<TokenResponse> SignUp(string email, string password, long phone) =>
        auth.RegisterAsync(new RegisterRequest
        {
            Email = email,
            Password = password,
            Phone = phone
        });
}
```

### Register

Creates (or upserts) the user, stores the hashed password, assigns the `User` role, and returns a fresh token pair.

```csharp
var tokens = await auth.RegisterAsync(new RegisterRequest
{
    Email = "jane@example.com",
    Password = "S3cur3P@ssw0rd!",
    Phone = 15551234567
});

// tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt
```

Throws `Exception("User already registered with that email")` if the email already exists.

### Login

Validates the password against the stored hash and returns a new token pair.

```csharp
var tokens = await auth.LoginAsync(new LoginRequest
{
    Email = "jane@example.com",
    Password = "S3cur3P@ssw0rd!"
});
```

Throws `Exception("No user with that email")` or `Exception("Invalid password")` on failure.

### Refresh

Exchanges a valid, non-revoked, non-expired refresh token for a new token pair. The presented token is revoked (rotated) as part of the call.

```csharp
var tokens = await auth.RefreshTokenAsync(oldRefreshToken);
```

Throws `Exception("Invalid refresh token")` if the token is missing, expired, or already revoked; `Exception("User no longer exists")` if the owning user was deleted.

### Wiring up endpoints

Minimal API example:

```csharp
app.MapPost("/auth/register", async (RegisterRequest req, IAuthService auth, CancellationToken ct) =>
    Results.Ok(await auth.RegisterAsync(req, ct)));

app.MapPost("/auth/login", async (LoginRequest req, IAuthService auth, CancellationToken ct) =>
    Results.Ok(await auth.LoginAsync(req, ct)));

app.MapPost("/auth/refresh", async (string refreshToken, IAuthService auth, CancellationToken ct) =>
    Results.Ok(await auth.RefreshTokenAsync(refreshToken, ct)));

// Protected endpoint — requires a valid access token from step 3
app.MapGet("/me", (ClaimsPrincipal user) =>
        Results.Ok(new { Id = user.FindFirstValue(ClaimTypes.NameIdentifier) }))
   .RequireAuthorization();
```

---

## API reference

### `IAuthService`

```csharp
public interface IAuthService
{
    Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
```

| Method | Description |
| ------ | ----------- |
| `RegisterAsync` | Creates the user + password hash, assigns the `User` role, returns a token pair. |
| `LoginAsync` | Verifies credentials and returns a token pair. |
| `RefreshTokenAsync` | Rotates a refresh token and returns a new token pair. |

### `AuthOptions`

```csharp
public class AuthOptions
{
    public string SecretKey { get; set; }          // HMAC-SHA256 signing key (>= 32 bytes)
    public string Issuer { get; set; }             // "iss" claim
    public string Audience { get; set; }           // "aud" claim
    public int    JwtExpiresMinutes { get; set; } = 60; // access-token lifetime
}
```

| Property | Default | Notes |
| -------- | ------- | ----- |
| `SecretKey` | — | Symmetric key for `HmacSha256`. Must be ≥ 256 bits. |
| `Issuer` | — | Written to the token and validated by the bearer middleware. |
| `Audience` | — | Written to the token and validated by the bearer middleware. |
| `JwtExpiresMinutes` | `60` | Access-token lifetime in minutes. The refresh token lifetime is fixed at **7 days** (see below). |

### Request / response DTOs

```csharp
public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public long   Phone { get; set; }
    public string FirstName { get; set; }  // see Behavioral notes
    public string LastName  { get; set; }  // see Behavioral notes
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class TokenResponse
{
    public string   AccessToken { get; set; }   // signed JWT
    public string   RefreshToken { get; set; }  // opaque GUID string ("N" format)
    public DateTime ExpiresAt { get; set; }      // access-token expiry (UTC)
}
```

### `UserRefreshToken` entity

```csharp
[Table(nameof(UserRefreshToken), Schema = "sec")]
public class UserRefreshToken : IEntity
{
    public int       Id { get; set; }
    public int       UserId { get; set; }
    public string    Token { get; set; }
    public DateTime  ExpiresAt { get; set; }
    public DateTime  CreatedAt { get; set; }
    public string?   CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string?   RevokedByIp { get; set; }
    public string?   ReplacedByToken { get; set; }
}
```

Query it with `UserRefreshTokenFilter`:

```csharp
var token = (await tokenRepo.GetAll(UserRefreshTokenFilter.ByToken(value))).FirstOrDefault();
```

### `PasswordHashExtensions`

String extension helpers built on ASP.NET Core Identity's `PasswordHasher<User>`:

```csharp
string hash   = "myPassword".HashPassword();        // hash a plaintext password
bool   isValid = storedHash.VerifyPassword("myPassword"); // verify against a stored hash
```

`AuthService` uses these internally; you rarely call them directly.

---

## How it works

```text
RegisterAsync / LoginAsync / RefreshTokenAsync
        │
        ▼
GenerateTokenResponse(user)
        │
        ├─ Build claims:  NameIdentifier = user.Id, Email = user.Email
        ├─ Sign JWT:      HmacSha256 with AuthOptions.SecretKey
        │                 iss = Issuer, aud = Audience, exp = now + JwtExpiresMinutes
        ├─ Create refresh token: GUID ("N"), ExpiresAt = now + 7 days
        ├─ Persist refresh token (sec.UserRefreshToken)
        └─ Return TokenResponse { AccessToken, RefreshToken, ExpiresAt }
```

- **Register** — rejects duplicate emails, upserts the `User` (matched on `Email`), stores the hashed password in `UserPassword`, assigns `RoleType.User`, then issues tokens.
- **Login** — loads the password record by email, verifies the hash, loads the user, then issues tokens.
- **Refresh** — looks up the presented refresh token; rejects it if missing, expired (`ExpiresAt < now`), or revoked (`RevokedAt` set); marks it revoked, then issues a new pair (token rotation).

---

## Behavioral notes & limitations

These reflect the current implementation — keep them in mind when integrating:

- **Roles are not yet placed in the JWT.** The token carries only `NameIdentifier` and `Email` claims. If you authorize by role, add the role claim when issuing or enrich the principal from the database.
- **`FirstName` / `LastName` on `RegisterRequest` are accepted but not persisted** by `RegisterAsync` — only `Email` and `Phone` are written to the `User`. Update the profile separately via the Users component if you need these stored.
- **Refresh-token lifetime is hard-coded to 7 days** and is not configurable through `AuthOptions`.
- **`RefreshTokenAsync` sets `ReplacedByToken` to a new random GUID**, not to the value of the newly issued refresh token, so the rotation chain is not directly linkable.
- **`CreatedByIp` / `RevokedByIp` are never populated** by the service; capture and set them yourself if you need IP auditing.
- **Errors are thrown as plain `Exception`** with English messages. Catch and translate them into the appropriate HTTP responses (e.g. `400`/`401`) at your API boundary.

---

## Security guidance

- Store `SecretKey` in user-secrets, environment variables, or a secrets manager — **never** commit it.
- Use HTTPS so access and refresh tokens are never sent in clear text.
- Treat the refresh token as a credential: store it in an `HttpOnly`, `Secure` cookie or secure client storage, and rotate on every use (the component already revokes the old one).
- Keep access-token lifetimes short (`JwtExpiresMinutes`) and rely on refresh for longevity.
- Consider a background job to purge expired/revoked rows from `sec.UserRefreshToken`.

---

## See also

- [Components overview](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/components/README.md)
- [Users component](../Users/README.md) — the `User`, `UserPassword`, and role model this package builds on
- [Dependency Injection](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/core/dependency-injection.md) — how auto-discovery registers `IAuthService`
- [Repositories](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/core/repositories.md) — the repository interfaces `AuthService` depends on

## License

MIT
