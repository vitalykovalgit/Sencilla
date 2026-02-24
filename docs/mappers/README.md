# Mappers

**Source:** `libs/mappers/`

> **Status:** Documentation in progress.

The Mappers module provides data transformation utilities: raw SQL mapping, entity-to-entity mapping, and Excel import/export.

---

## Packages

| Package | Purpose |
| ------- | ------- |
| `Sencilla.Mapper.Sql` | Expression-based SQL query builder with multi-DB support (SQL Server, MySQL) |
| `Sencilla.Mapper.Entity` | Entity-to-entity mapping (DTO ↔ domain model) |
| `Sencilla.Mapper.Excel` | Excel file import and export |

---

## SQL Mapper — `Sencilla.Mapper.Sql`

The SQL mapper is the engine behind `Sencilla.Repository.SqlMapper`. See [SQL Mapper Repository](../repositories/sql-mapper.md) for the repository-level API.

### Internal Components

```text
Extension/     DbConnection extensions (Query, Execute, ExecuteScalar)
Mapping/       TableMapping, ColumnMapping — define entity ↔ schema
Set/           DbSet, QuerySet, SpSet — query entry points
Context/       DbContext — connection management + pooling
Expression/    SqlExpression — LINQ-to-SQL translation
DbProvider/    SqlServerProvider, MySqlProvider
```

### Supported Databases

| Database | Provider |
| -------- | -------- |
| SQL Server | `SqlServerProvider` |
| MySQL | `MySqlProvider` |

### Example — Direct SQL query

```csharp
var dbContext = serviceProvider.GetRequiredService<SqlDbContext>();

var products = await dbContext
    .Query<Product>("SELECT * FROM Products WHERE CategoryId = @id AND Price > @minPrice",
        new { id = 5, minPrice = 10m })
    .ToListAsync();
```

### Example — Expression query builder

```csharp
var expr = new SqlExpression<Product>();
expr.Where(p => p.CategoryId == 5 && p.Price > 10);
expr.OrderBy(p => p.Name);
expr.Skip(0).Take(20);

string sql = expr.ToSelectSql();
var @params = expr.GetParameters();
```

### Example — Stored procedure

```csharp
var results = await dbContext
    .StoredProc<Product>("dbo.SearchProducts",
        new { Keyword = "widget", MaxPrice = 100m })
    .ToListAsync();
```

---

## Entity Mapper — `Sencilla.Mapper.Entity`

> **Status:** Implementation in progress.

Maps between entity types and DTOs (data transfer objects). Useful for separating your domain model from your API contracts.

```csharp
// Planned API
var dto = mapper.Map<ProductDto>(product);
var entity = mapper.Map<Product>(createRequest);

// Batch mapping
var dtos = mapper.MapList<ProductDto>(products);
```

---

## Excel Mapper — `Sencilla.Mapper.Excel`

Import from and export to Excel files.

### Export

```csharp
// Export a list of products to Excel
var excelBytes = await excelMapper.ExportAsync(
    products,
    sheet: "Products",
    columns: [
        new ExcelColumn<Product>("Name", p => p.Name),
        new ExcelColumn<Product>("Price", p => p.Price.ToString("C")),
        new ExcelColumn<Product>("Category", p => p.Category?.Name ?? "")
    ]);

return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    "products.xlsx");
```

### Import

```csharp
// Parse Excel rows into objects
var rows = await excelMapper.ImportAsync<ProductImportRow>(stream, sheet: "Products");

foreach (var row in rows)
{
    await productRepo.Create(new Product
    {
        Name = row.Name,
        Price = row.Price
    });
}
```

---

## Coming Soon

- Full Entity Mapper API reference and configuration
- Excel column validation during import
- Custom cell formatters for export
- Large file streaming for Excel export

---

## See Also

- [SQL Mapper Repository](../repositories/sql-mapper.md) — using SQL mapper as a repository
- [Entity Framework Repository](../repositories/entity-framework.md) — ORM alternative
