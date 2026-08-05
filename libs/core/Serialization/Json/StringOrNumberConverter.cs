namespace Sencilla.Core;

/// <summary>Deserialises a JSON field that is either a string or a number into its string representation.</summary>
public sealed class StringOrNumberConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
    {
        JsonTokenType.String => reader.GetString(),
        JsonTokenType.Number => reader.TryGetInt64(out var i)
            ? i.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : reader.GetDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        JsonTokenType.Null   => null,
        _                    => throw new JsonException($"Unexpected token {reader.TokenType} when parsing a string-or-number field.")
    };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
