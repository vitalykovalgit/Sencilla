
namespace Sencilla.Core;

/// <summary>
/// Binds an empty/whitespace string (or a JSON <c>null</c>) to <c>null</c> for an optional
/// <see cref="Guid"/> — typically an optional foreign key.
///
/// Keeps an unset optional reference as <c>null</c> instead of a bogus <see cref="Guid.Empty"/>
/// value that would fail (or silently corrupt) a foreign-key constraint. Registering this converter
/// for <c>Guid?</c> means System.Text.Json no longer uses its built-in <see cref="Nullable{T}"/>
/// wrapper for <c>Guid?</c>, so this type owns both read and write of the nullable value.
///
/// For a non-nullable key use <see cref="EmptyOrNullGuidConverter"/>.
/// </summary>
public sealed class NullableEmptyGuidConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // A bare null token is not routed here by default (HandleNull is false for Nullable<T>);
        // System.Text.Json assigns null directly. The branch is kept as a defensive no-op.
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing a nullable Guid.");
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
