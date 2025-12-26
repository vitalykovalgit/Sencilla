namespace Sencilla.Core.Serialization.Json
{
    public class JsonArrayEnumConverter<TEnum> : JsonConverter<TEnum[]> where TEnum : struct, Enum
    {
        public override TEnum[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException(typeToConvert.Name);

            var numbers = JsonSerializer.Deserialize<int[]>(ref reader, options) ?? Array.Empty<int>();

            return numbers?.Select(n =>
            {
                if (!Enum.IsDefined(typeof(TEnum), n))
                    throw new JsonException(typeof(TEnum).Name);

                return (TEnum)Enum.ToObject(typeof(TEnum), n);
            })?.ToArray();
        }

        public override void Write(Utf8JsonWriter writer, TEnum[] value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var v in value)
                writer.WriteNumberValue(Convert.ToInt32(v));
            writer.WriteEndArray();
        }
    }
}
