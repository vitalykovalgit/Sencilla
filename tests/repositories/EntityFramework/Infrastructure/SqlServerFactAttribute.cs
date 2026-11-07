namespace Sencilla.Repository.EntityFramework.Tests.Infrastructure;

/// <summary>
/// A [Fact] that needs a real SQL Server: upsert, merge and get-or-create are hand-built T-SQL MERGEs, which neither
/// in-memory provider runs. Skipped unless <see cref="Variable"/> holds a connection string to a server the test may
/// create databases on — each test makes its own and drops it.
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string Variable = "SENCILLA_TEST_SQLSERVER";

    public static string? ConnectionString => Environment.GetEnvironmentVariable(Variable);

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrEmpty(ConnectionString))
            Skip = $"Set {Variable} to a SQL Server connection string.";
    }
}
