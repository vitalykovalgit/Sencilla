
namespace Sencilla.Core;

/// <summary>
/// Binds an empty/whitespace string or a JSON <c>null</c> to <see cref="Guid.Empty"/> for a
/// non-nullable <see cref="Guid"/> — typically an entity key.
///
/// Clients (e.g. the TypeScript entity layer) send <c>id: ""</c> for a not-yet-persisted entity.
/// Without this, model binding cannot parse <c>""</c> into a non-nullable <see cref="Guid"/>, the
/// request body deserializes to <c>null</c>, and the create pipeline throws. Collapsing the unset
/// value to <see cref="Guid.Empty"/> lets store/EF key generation (sequential GUID /
/// <c>NEWSEQUENTIALID()</c>) produce the real id.
///
/// For an optional <c>Guid?</c> use <see cref="NullableEmptyGuidConverter"/>, which collapses the
/// unset value to <c>null</c> instead so optional foreign keys stay unset rather than becoming a
/// bogus all-zeros reference.
/// </summary>
public sealed class EmptyOrNullGuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // System.Text.Json routes the Null token here for non-nullable value types
        // (HandleNullOnRead defaults to true), so an explicit null id is handled too.
        if (reader.TokenType == JsonTokenType.Null)
            return Guid.Empty;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return string.IsNullOrWhiteSpace(value) ? Guid.Empty : Guid.Parse(value);
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing a Guid.");
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
