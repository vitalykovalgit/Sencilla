namespace Sencilla.Component.Tags;

/// <summary>
/// Key stringification for the shared tag table, whose <c>EntityId</c> is an <c>NVARCHAR(64)</c> that has to
/// hold whatever primary key the tagged entity uses (the same trade the audit log makes).
/// </summary>
internal static class TagKey
{
    public static string Text<TKey>(TKey id) => id?.ToString() ?? "";

    /// <summary>
    /// Parses a stored id back to the entity's key type. Returns false for rows that cannot be parsed — a
    /// mangled or stale row narrows nothing instead of failing the whole read.
    /// </summary>
    public static bool TryParse<TKey>(string text, out TKey key)
    {
        key = default!;

        if (string.IsNullOrEmpty(text))
            return false;

        var type = typeof(TKey);

        try
        {
            if (type == typeof(Guid))
            {
                if (!Guid.TryParse(text, out var guid))
                    return false;

                key = (TKey)(object)guid;
                return true;
            }

            key = (TKey)Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception e) when (e is FormatException or OverflowException or InvalidCastException)
        {
            return false;
        }
    }
}
