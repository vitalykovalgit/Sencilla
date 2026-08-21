using System.Security.Cryptography;
using System.Text;

namespace Sencilla.Component.I18n;

/// <summary>
/// Fingerprint of a source string, stored on every translation produced from it.
///
/// SHA-256 over the UTF-8 bytes, hex, lower-case. It is a change detector, not a security
/// primitive — what matters is that it is stable across processes, cultures and machines, which
/// rules out <c>string.GetHashCode()</c> (randomised per process since .NET Core).
///
/// Whitespace is trimmed and internal runs collapsed before hashing: re-indenting a source string
/// in the admin textarea is not a translation-invalidating change, and treating it as one would
/// re-run the whole catalog through a paid provider.
/// </summary>
public static class TranslationHash
{
    public static string Of(string? text)
    {
        var normalized = Normalize(text);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>True when <paramref name="stored"/> was produced from text that no longer matches.</summary>
    public static bool IsStale(string? stored, string? sourceText)
    {
        // A row with no recorded hash predates provenance: it cannot be PROVEN current, and a
        // re-run is cheaper than shipping a translation of text nobody can account for.
        if (string.IsNullOrEmpty(stored))
            return true;

        return !string.Equals(stored, Of(sourceText), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return Regex.Replace(text.Trim(), @"\s+", " ");
    }
}
