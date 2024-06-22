
namespace Sencilla.Core;

/// <summary>
/// Convert json to object and object to json 
/// </summary>
public class JsonObjectConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.StartObject)
            return JsonSerializer.Serialize(JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options), options);

        reader.Skip();

        return default;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(value, options);

        JsonSerializer.Serialize(writer, obj, options);
    }
}
