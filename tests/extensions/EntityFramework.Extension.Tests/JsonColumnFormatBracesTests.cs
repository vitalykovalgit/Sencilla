namespace Sencilla.EntityFramework.Extension.Tests;

/// <summary>
/// Regression guard for Photoboost WI#419: a JSON string column (e.g. a clipart's <c>Attrs</c>) is inlined
/// into the MERGE command text by the upsert/merge builders, and EF's <c>RawSqlCommandBuilder.Build</c>
/// unconditionally pipes that text through <see cref="string.Format(string, object[])"/> — so an unescaped
/// <c>{</c> blew up with FormatException before the command ever reached SQL Server (HTTP 500 on
/// <c>POST {api}/upsert</c>).
/// </summary>
public class JsonColumnFormatBracesTests
{
    private const string Json = """{"d":"M 0 0 H 1 V 1 H 0 Z","defaultColor":"#B71C1C"}""";

    private static string BuildUpsertSql()
    {
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Phone = 8098,
            // stands in for a [JsonObjectString] column — the builder writes it as a literal N'...'
            Email = Json,
            FirstName = "test",
            LastName = "test",
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now,
        };

        return new UpsertQueryBuilder<TestEntity>(new UpsertCommand<TestEntity>(te => te.Id))
            .Build([entity]);
    }

    [Fact]
    public void RawUpsertSql_WithJsonColumn_BreaksStringFormat()
    {
        // Documents WHY the escape exists: this is exactly what EF does to the command text.
        var raw = BuildUpsertSql();

        Assert.Contains(Json, raw);
        Assert.Throws<FormatException>(() => string.Format(raw, []));
    }

    [Fact]
    public void EscapeFormatBraces_MakesJsonUpsertSqlFormatSafe()
    {
        var raw = BuildUpsertSql();

        var escaped = EntityFrameworkCoreExtensions.EscapeFormatBraces(raw);

        // string.Format must both succeed AND give back the original SQL, braces intact.
        Assert.Equal(raw, string.Format(escaped, []));
    }

    [Fact]
    public void EscapeFormatBraces_LeavesBraceFreeSqlUntouched()
    {
        const string sql = "MERGE [dbo].[TestEntity] AS t USING (VALUES (1)) AS s (Id) ON t.Id = s.Id;";

        Assert.Equal(sql, EntityFrameworkCoreExtensions.EscapeFormatBraces(sql));
    }
}
