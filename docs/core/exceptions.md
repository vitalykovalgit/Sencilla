# Exceptions

**Namespace:** `Sencilla.Core`
**Source:** `libs/core/Exceptions/`

Sencilla provides a typed exception hierarchy that maps directly to HTTP status codes. Throwing one of these exceptions in any layer of your application (service, repository, handler) produces a predictable HTTP response when you use Sencilla's error handling middleware.

---

## Exception Hierarchy

```text
Exception
└── SencillaException                  Base for all framework exceptions
    ├── BadRequestException            400 Bad Request
    ├── UnauthorizedException          401 Unauthorized
    ├── ForbiddenException             403 Forbidden
    ├── NotFoundException              404 Not Found
    └── InternalServerErrorException   500 Internal Server Error
```

---

## `SencillaException` — Base

```csharp
public class SencillaException : Exception
{
    public int StatusCode { get; }
    public string? Detail { get; }

    public SencillaException(int statusCode, string message, string? detail = null)
        : base(message)
    {
        StatusCode = statusCode;
        Detail = detail;
    }
}
```

All derived exceptions set `StatusCode` automatically.

---

## Built-in Exceptions

### `BadRequestException` — HTTP 400

Use when the request contains invalid data.

```csharp
public class BadRequestException : SencillaException
{
    public BadRequestException(string message, string? detail = null)
        : base(400, message, detail) { }
}
```

**Example:**
```csharp
if (request.Price <= 0)
    throw new BadRequestException("Price must be positive", $"Received: {request.Price}");
```

---

### `UnauthorizedException` — HTTP 401

Use when the caller is not authenticated.

```csharp
public class UnauthorizedException : SencillaException
{
    public UnauthorizedException(string message = "Authentication required")
        : base(401, message) { }
}
```

**Example:**
```csharp
if (currentUser is null)
    throw new UnauthorizedException();
```

---

### `ForbiddenException` — HTTP 403

Use when the caller is authenticated but lacks permission.

```csharp
public class ForbiddenException : SencillaException
{
    public ForbiddenException(string message = "Access denied")
        : base(403, message) { }
}
```

**Example:**
```csharp
if (!currentUser.HasPermission("products.delete"))
    throw new ForbiddenException("You do not have permission to delete products");
```

---

### `NotFoundException` — HTTP 404

Use when a requested resource does not exist.

```csharp
public class NotFoundException : SencillaException
{
    public NotFoundException(string message = "Resource not found")
        : base(404, message) { }
}
```

**Example:**
```csharp
var product = await _repo.GetById(id)
    ?? throw new NotFoundException($"Product {id} not found");
```

---

### `InternalServerErrorException` — HTTP 500

Use for unrecoverable application errors that are not the caller's fault.

```csharp
public class InternalServerErrorException : SencillaException
{
    public InternalServerErrorException(string message = "An unexpected error occurred")
        : base(500, message) { }
}
```

**Example:**
```csharp
try
{
    await externalService.ProcessAsync();
}
catch (ExternalServiceException ex)
{
    throw new InternalServerErrorException("Payment gateway unavailable");
}
```

---

## Custom Exceptions

Extend `SencillaException` for domain-specific errors:

```csharp
// 409 Conflict — for optimistic concurrency violations
public class ConflictException : SencillaException
{
    public ConflictException(string message)
        : base(409, message) { }
}

// 422 Unprocessable Entity — for business rule violations
public class BusinessRuleException : SencillaException
{
    public string RuleCode { get; }

    public BusinessRuleException(string ruleCode, string message)
        : base(422, message)
    {
        RuleCode = ruleCode;
    }
}
```

---

## Error Handling Middleware

Register Sencilla's exception handler to convert `SencillaException` into `ProblemDetails` responses automatically:

```csharp
// Program.cs
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        if (exception is SencillaException sencillaEx)
        {
            context.Response.StatusCode = sencillaEx.StatusCode;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = sencillaEx.StatusCode,
                Title = sencillaEx.Message,
                Detail = sencillaEx.Detail
            });
        }
        else
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 500,
                Title = "An unexpected error occurred"
            });
        }
    });
});
```

Or use ASP.NET Core's built-in `IProblemDetailsService` approach:

```csharp
builder.Services.AddProblemDetails();
app.UseExceptionHandler();
```

---

## Usage Pattern in Handlers

```csharp
[Implement]
public class UpdateProductHandler : ICommandHandler<UpdateProductCommand, Product>
{
    private readonly IUpdateRepository<Product, int> _products;

    public async Task<Product> HandleAsync(UpdateProductCommand cmd, CancellationToken token)
    {
        // 400 — validate input
        if (cmd.Price <= 0)
            throw new BadRequestException("Price must be greater than zero");

        // 404 — check existence
        var product = await _products.GetById(cmd.ProductId)
            ?? throw new NotFoundException($"Product {cmd.ProductId} not found");

        // Business logic
        product.Price = cmd.Price;
        return (await _products.Update(product, token))!;
    }
}
```

---

## See Also

- [Commands & Events](commands-events.md) — where exceptions are typically thrown
- [Getting Started](../getting-started.md) — error handler registration
