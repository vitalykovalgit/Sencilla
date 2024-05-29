namespace Sencilla.Repository.EntityFramework.Extension;

public static class SqlSanitizer
{
    private static readonly string _pattern = @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE( +INTO)?|SELECT|UPDATE( +TOP)?|UNION( +ALL)?|WHERE|ORDER( +BY)?|GROUP( +BY)?|FROM|HAVING|CASE( +WHEN)?|AND|OR|LIKE|LIMIT|OFFSET)\b)|(--.*)|(/\*(.|[\r\n])*?\*/)";

    public static string Sanitize(string input)
    {
        input = input.Trim();

        if (ContainsSqlInjection(input))
        {
            throw new ArgumentException("SQL injection attempt detected!");
        }

        return input;
    }

    private static bool ContainsSqlInjection(string input) => Regex.IsMatch(input, _pattern, RegexOptions.IgnoreCase);
}
