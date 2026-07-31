
namespace Sencilla.Component.Tags;

/// <summary>
/// The one normalisation rule for tag names, shared by every tag repository, the generic tag endpoints and any
/// consumer that resolves a tag by name.
///
/// <para>Lowercase + trim + dedupe + ordinal sort. Two reasons it is not optional: consumers compare tags
/// ordinally (case-sensitively) while SQL Server compares under a case-insensitive collation, so an
/// un-normalised <c>Summer</c> is a silent miss in one and a hit in the other; and a deterministic (sorted)
/// set makes the stored bytes deterministic, which is what keeps cross-runtime test vectors stable.</para>
///
/// <para>Invalid input is rejected with an error CODE, never silently rewritten — a caller that meant
/// <c>black friday</c> should learn that spaces are not tags, not receive <c>black-friday</c> by surprise.</para>
/// </summary>
public static class TagName
{
    /// <summary>Longest single tag.</summary>
    public const int MaxLength = 64;

    /// <summary>Longest serialised set — matches the inline column's <c>NVARCHAR(4000)</c>.</summary>
    public const int MaxSetLength = 4000;

    /// <summary>
    /// Normalises one tag: trimmed, lowercased, validated against <c>a-z 0-9 - _ . :</c> (the colon exists for
    /// namespacing, e.g. <c>promo:black-friday</c>).
    /// </summary>
    /// <exception cref="BadRequestException">
    /// <c>tag-empty</c> — blank after trimming; <c>tag-too-long</c> — over <see cref="MaxLength"/>;
    /// <c>tag-invalid</c> — contains anything outside the allowed set.
    /// </exception>
    public static string One(string? tag)
    {
        var normalized = tag?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalized))
            throw new BadRequestException("tag-empty");

        if (normalized.Length > MaxLength)
            throw new BadRequestException("tag-too-long");

        foreach (var c in normalized)
            if (!IsAllowed(c))
                throw new BadRequestException("tag-invalid");

        return normalized;
    }

    /// <summary>
    /// Normalises a whole set: every tag through <see cref="One"/>, deduped, ordinally sorted, and checked
    /// against the serialised-length ceiling so an over-long set fails as a 400 instead of a SQL truncation 500.
    /// </summary>
    /// <exception cref="BadRequestException">Any code from <see cref="One"/>, or <c>tag-set-too-large</c>.</exception>
    public static List<string> Set(IEnumerable<string?>? tags)
    {
        if (tags == null)
            return [];

        var normalized = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var tag in tags)
            normalized.Add(One(tag));

        // The inline repository serialises the set as a JSON array; the other repositories are per-row and cannot overflow,
        // but one ceiling for all three keeps "what fits" answerable without knowing the storage.
        var length = 2 + normalized.Sum(t => t.Length + 3);   // ["a","b"] — quotes, commas, brackets
        if (length > MaxSetLength)
            throw new BadRequestException("tag-set-too-large");

        return [.. normalized];
    }

    /// <summary>Normalises a lookup argument, returning null instead of throwing — a malformed query filter or
    /// DSL argument matches nothing rather than failing the whole request.</summary>
    public static string? TryOne(string? tag)
    {
        var normalized = tag?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalized) || normalized.Length > MaxLength)
            return null;

        foreach (var c in normalized)
            if (!IsAllowed(c))
                return null;

        return normalized;
    }

    /// <summary>Normalises lookup arguments, dropping the malformed ones.</summary>
    public static string[] TrySet(IEnumerable<string?>? tags)
        => tags == null ? [] : [.. tags.Select(TryOne).Where(t => t != null).Distinct(StringComparer.Ordinal)!];

    private static bool IsAllowed(char c)
        => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.' or ':';
}
