# SQL Mapper Repository

[Home](../../README.md) / [Docs](../index.md) / [Repositories](README.md) / SQL Mapper

**NuGet:** `Sencilla.Repository.SqlMapper`
**Namespace:** `Sencilla.Repository.SqlMapper`
**Source:** `libs/repositories/SqlMapper/`

The SQL Mapper repository uses raw SQL with an expression-based query builder. Use it when:

- You need fine-grained SQL control that EF Core abstracts away
- You are working with stored procedures or complex native queries
- EF Core migrations are not an option (legacy schema, cross-database views)
- Performance is critical and you want to avoid EF Core overhead

---

## Installation

```bash
dotnet add package Sencilla.Repository.SqlMapper
```

---

## Internal Architecture

The mapper is built in layers:

```text
0. Extension/     DbConnection and DataReader extensions
1. Mapping/       TableMapping, ColumnMapping (entity ↔ SQL schema)
2. Set/           DbSet, QuerySet, SpSet (stored proc) — query entry points
3. Context/       DbContext — connection and unit-of-work
4. Expression/    SqlExpression — LINQ to SQL query builder
5. DbProvider/    SqlServerProvider, MySqlProvider — DB-specific SQL generation
```

---

## Database Support

| Database | Provider class |
| -------- | -------------- |
| SQL Server | `SqlServerProvider` |
| MySQL | `MySqlProvider` |

---

## Table Mapping

Map your entity to a database table:

```csharp
public class ProductMapping : TableMapping<Product>
{
    public ProductMapping()
    {
        ToTable("dbo.Products");

        MapColumn(x => x.Id).AsKey().AsIdentity();
        MapColumn(x => x.Name).HasColumnName("ProductName").HasMaxLength(200);
        MapColumn(x => x.Price).HasColumnName("UnitPrice");
        MapColumn(x => x.CreatedDate).HasColumnName("created_date");
    }
}
```

### `ColumnMapping` options

| Method | Effect |
| ------ | ------ |
| `AsKey()` | Marks as primary key |
| `AsIdentity()` | Database-generated identity (auto-increment) |
| `HasColumnName(name)` | Override column name (default: property name) |
| `HasMaxLength(n)` | Set string column max length |
| `Ignore()` | Exclude from SQL operations |
| `HasConversion(to, from)` | Custom type converter |

---

## Query Execution

### `DbSet<TEntity>` — table-based queries

```csharp
var dbContext = serviceProvider.GetRequiredService<DbContext>();

// Simple query
var products = await dbContext
    .Set<Product>()
    .Where(p => p.Price > 10)
    .OrderBy(p => p.Name)
    .ToListAsync();

// With filter
var results = await dbContext
    .Set<Product>()
    .ApplyFilter(filter)
    .ToListAsync();
```

### `QuerySet` — raw SQL

```csharp
var results = await dbContext
    .Query<Product>("SELECT * FROM dbo.Products WHERE CategoryId = @categoryId",
        new { categoryId = 5 })
    .ToListAsync();
```

### `SpSet` — stored procedures

```csharp
var results = await dbContext
    .StoredProc<Product>("dbo.GetProductsByCategory",
        new { CategoryId = 5, MinPrice = 10.0m })
    .ToListAsync();
```

---

## `SqlExpression` — Query Builder

`SqlExpression` translates C# lambda expressions into parameterized SQL:

```csharp
var expr = new SqlExpression<Product>();
expr.Where(p => p.Price > 10 && p.CategoryId == 3);
expr.OrderBy(p => p.Name);
expr.Skip(0).Take(20);

string sql = expr.ToSelectSql();
// SELECT * FROM dbo.Products WHERE Price > @p0 AND CategoryId = @p1
// ORDER BY ProductName OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
```

---

## Implementing a Repository

```csharp
[Implement]
public class ProductRepository : IReadRepository<Product, int>
{
    private readonly DbContext _db;

    public ProductRepository(DbContext db) => _db = db;

    public async Task<IEnumerable<Product>> GetAll(IFilter? filter = null, params string[] with)
        => await _db.Set<Product>().ApplyFilter(filter).ToListAsync();

    public async Task<Product?> GetById(int id, params string[] with)
        => await _db.Set<Product>().FindAsync(id);

    // ... other interface methods
}
```

---

## `DbConnectionEx` — Connection Extensions

Low-level utilities on `IDbConnection`:

```csharp
// Execute and map results
var products = connection.Query<Product>(
    "SELECT * FROM Products WHERE CategoryId = @id",
    new { id = categoryId });

// Execute scalar
int count = connection.ExecuteScalar<int>(
    "SELECT COUNT(*) FROM Products");

// Execute non-query
connection.Execute(
    "UPDATE Products SET Price = @price WHERE Id = @id",
    new { price = 9.99m, id = 42 });
```

---

## `DbDataReaderEx` — Reader Extensions

```csharp
// Map a DataReader row to an entity using column mappings
while (await reader.ReadAsync())
{
    var product = reader.MapTo<Product>(mapping);
    results.Add(product);
}
```

---

## See Also

- [Core Repository Interfaces](../core/repositories.md) — the interface this implements
- [Entity Framework Repository](entity-framework.md) — higher-level ORM alternative
- [Filtering](../core/filtering.md) — `IFilter` → SQL translation

---

[Home](../../README.md) / [Docs](../index.md) / [Repositories](README.md) / **SQL Mapper**
