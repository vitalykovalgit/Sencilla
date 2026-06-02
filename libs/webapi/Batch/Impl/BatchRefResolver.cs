using System.Text.RegularExpressions;

namespace Sencilla.Web.Batch;

/// <summary>
/// Resolves <c>$ref:step.field</c> placeholders in a write step's body against the
/// results of earlier write steps. v1: top-level string fields only.
/// </summary>
internal static partial class BatchRefResolver
{
    [GeneratedRegex(@"^\$ref:([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)$")]
    private static partial Regex RefPattern();

    /// <summary>
    /// Replaces each top-level <c>$ref</c> string in <paramref name="body"/> with the
    /// referenced field from <paramref name="priorResults"/>. Mutates <paramref name="body"/>.
    /// </summary>
    public static void Resolve(JsonObject body, IReadOnlyDictionary<string, JsonObject> priorResults)
    {
        foreach (var property in body.ToList())
        {
            if (property.Value is not JsonValue value || !value.TryGetValue<string>(out var raw))
                continue;

            var match = RefPattern().Match(raw);
            if (!match.Success)
                continue;

            var stepRef = match.Groups[1].Value;
            var field = match.Groups[2].Value;

            if (!priorResults.TryGetValue(stepRef, out var source))
                throw new BatchRefException($"$ref '{raw}': step '{stepRef}' produced no result.");

            var resolved = FindField(source, field)
                ?? throw new BatchRefException($"$ref '{raw}': field '{field}' not found on step '{stepRef}'.");

            body[property.Key] = resolved.DeepClone();
        }
    }

    private static JsonNode? FindField(JsonObject source, string field)
    {
        foreach (var kv in source)
            if (string.Equals(kv.Key, field, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        return null;
    }
}
