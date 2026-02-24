# Filtering

[Home](../../README.md) / [Docs](../index.md) / [Core](README.md) / Filtering

**Namespace:** `Sencilla.Core`
**Source:** `libs/core/Filter/`

`IFilter` is the standard way to pass **pagination, sorting, search, and property filters** to any repository method. Every `IReadRepository` method that accepts a filter uses `IFilter?`.

---

## `IFilter` Interface

```csharp
public interface IFilter
{
    // Pagination
    int Skip { get; set; }       // How many records to skip (offset). Default: 0.
    int Take { get; set; }       // How many records to return. Default: 0 (all).

    // Sorting
    string[]? OrderBy { get; set; }   // Column/property names to sort by.
    bool Descending { get; set; }     // true = descending, false = ascending.

    // Aggregations (used with GetSum, GetMax, GetMin, GetAverage)
    string? Aggregate { get; set; }   // The property name to aggregate on.

    // Eager loading
    string[]? With { get; set; }      // Navigation property names to include.

    // Full-text search
    string? Search { get; set; }      // Applied across searchable string fields.

    // Property-level filters (key = property name)
    Dictionary<string, FilterProperty>? Properties { get; set; }

    // Add a property filter programmatically
    IFilter AddProperty(string name, FilterPropertyType type, params object[] values);
}
```

---

## `FilterProperty`

Represents a filter condition on a single property.

```csharp
public class FilterProperty
{
    public string Name { get; set; }                // Property name (e.g., "Price")
    public FilterPropertyType Type { get; set; }    // The condition type
    public object[] Values { get; set; }            // One or more values
}
```

### `FilterPropertyType` Enum

| Value | SQL equivalent | Values required | Example |
| ----- | -------------- | --------------- | ------- |
| `Equal` | `= value` | 1 | Price is exactly 9.99 |
| `NotEqual` | `!= value` | 1 | Status is not "Inactive" |
| `Greater` | `> value` | 1 | CreatedDate after date |
| `GreaterOrEqual` | `>= value` | 1 | Age ≥ 18 |
| `Less` | `< value` | 1 | Stock < 5 |
| `LessOrEqual` | `<= value` | 1 | Price ≤ 100 |
| `Between` | `BETWEEN a AND b` | 2 | Price between 10 and 50 |
| `In` | `IN (a, b, c)` | 1+ | Status in ["Active", "Pending"] |
| `NotIn` | `NOT IN (...)` | 1+ | Category not in excluded list |
| `Contains` | `LIKE '%value%'` | 1 | Name contains "widget" |
| `StartsWith` | `LIKE 'value%'` | 1 | Name starts with "S" |
| `EndsWith` | `LIKE '%value'` | 1 | Email ends with "@corp.com" |
| `IsNull` | `IS NULL` | 0 | ParentId is null |
| `IsNotNull` | `IS NOT NULL` | 0 | UpdatedDate is not null |
| `Bool` | `= 1 / = 0` | 1 | IsAvailable = true |

---

## `Filter` — Concrete Implementation

Use the concrete `Filter` class (also in `Sencilla.Core`) to build filters:

```csharp
using Sencilla.Core;

var filter = new Filter();
```

---

## Usage Examples

### Pagination

```csharp
var page2 = new Filter
{
    Skip = 20,   // skip first 20
    Take = 20    // return next 20
};

var products = await repo.GetAll(page2);
```

### Sorting

```csharp
var sorted = new Filter
{
    OrderBy = ["Price", "Name"],   // sort by Price, then by Name
    Descending = false              // ascending
};
```

```csharp
// Descending (newest first)
var newest = new Filter
{
    OrderBy = ["CreatedDate"],
    Descending = true,
    Take = 10
};
```

### Search

```csharp
// Full-text search across searchable fields
var search = new Filter { Search = "widget" };
var results = await repo.GetAll(search);
```

### Property Filters

```csharp
// Single equality
var filter = new Filter();
filter.AddProperty("IsAvailable", FilterPropertyType.Bool, true);

// Price range
filter.AddProperty("Price", FilterPropertyType.Between, 10.0m, 100.0m);

// Status IN list
filter.AddProperty("Status", FilterPropertyType.In, "Active", "Pending");

// Null check
filter.AddProperty("DeletedDate", FilterPropertyType.IsNull);

var products = await repo.GetAll(filter);
```

### Eager Loading (Include Navigation Properties)

```csharp
// Include the Category and its parent
var product = await repo.GetById(id, "Category", "Category.Parent");

// Or in GetAll
var products = await repo.GetAll(filter, "Category", "Tags");
```

### Aggregations

```csharp
// Total sales value
var filter = new Filter();
filter.AddProperty("Status", FilterPropertyType.Equal, "Completed");
filter.Aggregate = "Amount";  // the property to sum

decimal totalSales = (decimal)await repo.GetSum(filter);
```

### Combined Filter

```csharp
var filter = new Filter
{
    Skip = 0,
    Take = 50,
    OrderBy = ["CreatedDate"],
    Descending = true,
    Search = "laptop"
};
filter.AddProperty("CategoryId", FilterPropertyType.In, 1, 2, 3);
filter.AddProperty("Price", FilterPropertyType.LessOrEqual, 2000m);
filter.AddProperty("IsAvailable", FilterPropertyType.Bool, true);

var products = await repo.GetAll(filter, "Category");
int total = await repo.GetCount(filter);
```

---

## API Endpoint Pattern

Expose filter parameters as query string in an API endpoint:

```csharp
app.MapGet("/products", async (
    [FromQuery] int skip = 0,
    [FromQuery] int take = 20,
    [FromQuery] string? search = null,
    [FromQuery] string? orderBy = null,
    [FromQuery] bool descending = false,
    IReadRepository<Product, int> repo) =>
{
    var filter = new Filter
    {
        Skip = skip,
        Take = take,
        Search = search,
        OrderBy = orderBy is not null ? [orderBy] : null,
        Descending = descending
    };

    var data = await repo.GetAll(filter);
    var total = await repo.GetCount(filter);

    return Results.Ok(new { data, total, skip, take });
});
```

### With ASP.NET Core model binding (Sencilla.Web)

`Sencilla.Web` provides a model binder so you can receive `IFilter` directly:

```csharp
// Controller action
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] Filter filter)
{
    var data = await _repo.GetAll(filter);
    return Ok(data);
}
```

---

## Working with `Properties` Dictionary Directly

Instead of `AddProperty`, you can populate the dictionary manually:

```csharp
var filter = new Filter
{
    Properties = new Dictionary<string, FilterProperty>
    {
        ["IsAvailable"] = new FilterProperty
        {
            Name = "IsAvailable",
            Type = FilterPropertyType.Bool,
            Values = [true]
        },
        ["CategoryId"] = new FilterProperty
        {
            Name = "CategoryId",
            Type = FilterPropertyType.In,
            Values = [1, 3, 5]
        }
    }
};
```

---

## See Also

- [Repository Interfaces](repositories.md) — methods that accept `IFilter`
- [Entity Framework Repository](../repositories/entity-framework.md) — how filters are translated to EF Core queries

---

[Home](../../README.md) / [Docs](../index.md) / [Core](README.md) / **Filtering**
