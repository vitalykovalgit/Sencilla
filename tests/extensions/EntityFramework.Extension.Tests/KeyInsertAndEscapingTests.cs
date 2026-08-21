using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.EntityFramework.Extension.Tests;

/// <summary>
/// The MERGE's INSERT clause must name [Id] exactly when the caller supplies the key, and string
/// values must be escaped the way T-SQL — not MySQL — requires.
/// </summary>
public class KeyInsertAndEscapingTests
{
    private static string BuildStringKeyed(string id = "pe.basket.head.title", string? description = "Кошик", StringKeyEntity? entity = null)
    {
        if (!RepositoryEntityFrameworkBootstrap.Entities.Contains(typeof(StringKeyEntity)))
            RepositoryEntityFrameworkBootstrap.Entities.Add(typeof(StringKeyEntity));

        var builder = new UpsertQueryBuilder<StringKeyEntity>(new UpsertCommand<StringKeyEntity>(e => e.Id));

        return builder.Build([entity ?? new StringKeyEntity { Id = id, Description = description }]);
    }

    private static string BuildIntKeyed()
    {
        if (!RepositoryEntityFrameworkBootstrap.Entities.Contains(typeof(TestEntityWithNavProps)))
            RepositoryEntityFrameworkBootstrap.Entities.Add(typeof(TestEntityWithNavProps));

        var builder = new UpsertQueryBuilder<TestEntityWithNavProps>(new UpsertCommand<TestEntityWithNavProps>(e => e.Id));

        return builder.Build([new TestEntityWithNavProps { Id = 1, Name = "n", ChildId = 2 }]);
    }

    /// A client-supplied key has no IDENTITY behind it: omitting [Id] made SQL Server reject the
    /// statement with «Cannot insert the value NULL into column 'Id'».
    [Fact]
    public void Insert_NamesIdColumn_ForClientSuppliedStringKey()
    {
        var insertClause = InsertClauseOf(BuildStringKeyed());

        Assert.Contains("[Id]", insertClause);
        Assert.Contains("s.[Id]", insertClause);
    }

    /// The mirror case: an int key IS an IDENTITY column, and naming it would break every entity
    /// that relies on the database allocating the id.
    [Fact]
    public void Insert_OmitsIdColumn_ForDatabaseGeneratedIntKey()
    {
        var insertClause = InsertClauseOf(BuildIntKeyed());

        Assert.DoesNotContain("[Id]", insertClause);
    }

    /// The key is what the rows were matched ON, so re-assigning it in the UPDATE branch is both
    /// pointless and, for a string key, a way to write NULL over a live primary key.
    [Fact]
    public void Update_NeverAssignsIdColumn()
    {
        var sql = BuildStringKeyed();
        var updateClause = sql[sql.IndexOf("WHEN MATCHED THEN UPDATE", StringComparison.Ordinal)..];

        Assert.DoesNotContain("t.[Id] =", updateClause);
    }

    /// T-SQL doubles a quote to escape it. The old `\'` left the literal open, so every Ukrainian
    /// value carrying an apostrophe — Сім'я, Пам'ятні — produced a syntax error.
    [Fact]
    public void Values_EscapeApostropheTheTSqlWay()
    {
        var sql = BuildStringKeyed(description: "Сім'я");

        Assert.Contains("N'Сім''я'", sql);
        Assert.DoesNotContain(@"\'", sql);
    }

    /// Doubling is also the containment: a value cannot close its own literal and append a statement.
    [Fact]
    public void Values_CannotEscapeTheLiteralToInjectSql()
    {
        var sql = BuildStringKeyed(description: "x'; DROP TABLE dbo.Resource; --");

        // The payload stays INSIDE one literal: its quote is doubled, so nothing after it is ever
        // parsed as SQL. An unescaped `N'x'; DROP…` would have closed the literal after «x».
        Assert.Contains("N'x''; DROP TABLE dbo.Resource; --'", sql);
        Assert.DoesNotContain("N'x'; DROP", sql);
    }

    /// A Guid key stays on the database-generated side: these columns are DEFAULT NEWSEQUENTIALID()
    /// and callers create with an EMPTY id, relying on the default. Naming [Id] would insert
    /// all-zeros, and the next create would MATCH that row and overwrite it rather than insert.
    [Fact]
    public void Insert_OmitsIdColumn_ForGuidKey()
    {
        if (!RepositoryEntityFrameworkBootstrap.Entities.Contains(typeof(TestEntity)))
            RepositoryEntityFrameworkBootstrap.Entities.Add(typeof(TestEntity));

        var builder = new UpsertQueryBuilder<TestEntity>(new UpsertCommand<TestEntity>(e => e.Id));
        var sql = builder.Build([new TestEntity { Id = Guid.NewGuid(), Email = "a@b.c" }]);

        Assert.DoesNotContain("[Id]", InsertClauseOf(sql));
    }

    /// `p.PropertyType` for a `DateTime?` is `Nullable<DateTime>`, so it matched no branch and fell
    /// through to a bare ToString() — an unquoted date, i.e. a syntax error.
    [Fact]
    public void Values_QuoteNullableDateTime()
    {
        var sql = BuildStringKeyed(entity: Row(updated: new DateTime(2026, 8, 21, 10, 29, 40, DateTimeKind.Utc)));

        Assert.Contains("'2026-08-21 10:29:40'", sql);
    }

    /// An enum fell through the same way and emitted its member NAME, which SQL Server parses as an
    /// identifier: «Invalid column name 'Machine'».
    [Fact]
    public void Values_EmitEnumAsItsNumericValue()
    {
        var sql = BuildStringKeyed(entity: Row(origin: SampleOrigin.Machine));

        Assert.DoesNotContain("Machine", sql);
        Assert.Contains(",1,", ValuesRowOf(sql));
    }

    /// Under a comma-decimal culture `12.5m.ToString()` is «12,5», which splits one value into two
    /// inside the VALUES list — a column-count error, or silent column shifting on money.
    [Fact]
    public void Values_FormatDecimalsInvariantly_UnderCommaDecimalCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA");

            var sql = BuildStringKeyed(entity: Row(price: 12.5m));

            Assert.Contains("12.5", sql);
            Assert.DoesNotContain("12,5", sql);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static StringKeyEntity Row(DateTime? updated = null, SampleOrigin origin = SampleOrigin.Unknown, decimal? price = null) =>
        new() { Id = "pe.zzz.key", Description = "d", UpdatedDate = updated, Origin = origin, Price = price };

    /// The single `(...)` row inside `USING (VALUES ... )`.
    private static string ValuesRowOf(string sql)
    {
        var start = sql.IndexOf("USING (VALUES", StringComparison.Ordinal);
        var end = sql.IndexOf("AS s (", StringComparison.Ordinal);

        return sql[start..end];
    }

    private static string InsertClauseOf(string sql)
    {
        var start = sql.IndexOf("WHEN NOT MATCHED BY TARGET THEN", StringComparison.Ordinal);
        var end = sql.IndexOf("WHEN MATCHED THEN UPDATE", StringComparison.Ordinal);

        return sql[start..end];
    }
}
