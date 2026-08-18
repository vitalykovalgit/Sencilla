using System.Buffers;

namespace Sencilla.Core;

/// <summary>
/// Bridges a column that HOLDS json text to a property that IS json on the wire: the API speaks objects,
/// the entity (and the database) keeps the raw text.
///
/// Reading accepts the three shapes a client legitimately sends — an object/array, the same json as a
/// string (the property is typed <c>string</c>, so serializers naturally emit one), or null to clear the
/// column. Anything else THROWS. It used to fall through to <c>reader.Skip()</c> and return null, which
/// meant a mis-shaped value was answered with 200 OK while the column was silently wiped: no error, no
/// warning, data gone. A malformed request must fail, never quietly destroy the row it was trying to save.
/// </summary>
public class JsonObjectStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            // The wire shape. Re-emitted straight from the parsed document rather than round-tripped
            // through Dictionary<string, object>, so numbers, nulls and nesting reach the column unchanged.
            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                return Compact(doc.RootElement);
            }

            // Explicitly clearing the column.
            case JsonTokenType.Null:
                return null;

            // The property is a string in C# and in the database, so plenty of clients send it as one.
            // Accept it — but only if it really is json: storing arbitrary text here would just move the
            // failure to the next read, when Write has to turn it back into an object.
            case JsonTokenType.String:
            {
                var raw = reader.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                    return null;

                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    return Compact(doc.RootElement);
                }
                catch (JsonException e)
                {
                    throw new JsonException(
                        $"Expected json for '{typeToConvert.Name}', but the string is not valid json.", e);
                }
            }

            default:
                throw new JsonException(
                    $"Expected a json object, array, string or null for '{typeToConvert.Name}', got {reader.TokenType}.");
        }
    }

    /// <summary>
    /// Normalises to compact json before it reaches the column. What arrives keeps the sender's whitespace,
    /// and several of these columns are NVARCHAR(4000) — an indented body from a json editor can be several
    /// times the size of the same data compacted, which is the difference between fitting and being
    /// truncated. Written through an explicit writer rather than the ambient options so it stays compact
    /// even where WriteIndented is on.
    /// </summary>
    private static string Compact(JsonElement element)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
            element.WriteTo(writer);

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // An empty column is null on the wire. Writing NOTHING (the previous behaviour) leaves the writer
        // expecting a value it never receives, which throws while serializing the response — a 500 on a
        // plain GET of a row whose json column happens to be blank.
        if (string.IsNullOrWhiteSpace(value))
        {
            writer.WriteNullValue();
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(value);
            doc.WriteTo(writer);
        }
        catch (JsonException)
        {
            // Legacy rows may hold text that was never json. Emitting it as a json string keeps the
            // response valid and the value visible, instead of failing the whole read.
            writer.WriteStringValue(value);
        }
    }
}
