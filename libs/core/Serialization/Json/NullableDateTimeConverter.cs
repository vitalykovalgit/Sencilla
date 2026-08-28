
namespace Sencilla.Core;

/// <summary>
/// Binds a JSON <c>null</c> or empty object <c>{}</c> to <see cref="DateTime.MinValue"/>
/// for a non-nullable <see cref="DateTime"/> — typically a server-stamped tracking field
/// such as <c>CreatedDate</c> on <see cref="IEntityCreateableTrack"/>.
///
/// Clients send <c>createdDate: null</c> or <c>createdDate: {}</c> for a not-yet-persisted
/// entity (the TypeScript <c>Date</c> default serializes to <c>{}</c> when unset).
/// Without this, model binding cannot parse these tokens into a non-nullable
/// <see cref="DateTime"/>, the request body deserializes to <c>null</c>, and the create
/// pipeline throws. Collapsing the unset value to <see cref="DateTime.MinValue"/> lets the
/// repository layer (<c>CreateRepository</c>) stamp the real <c>DateTime.UtcNow</c> value
/// before the record reaches the database.
/// </summary>
public sealed class NullableDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // null token: System.Text.Json routes this here for non-nullable value types
        // (HandleNullOnRead defaults to true).
        if (reader.TokenType == JsonTokenType.Null)
            return DateTime.MinValue;

        // Empty-object token: TypeScript Date serializes to {} when the field is
        // uninitialized (new Date() on a fresh entity before any value is assigned).
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            reader.Skip();
            return DateTime.MinValue;
        }

        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
