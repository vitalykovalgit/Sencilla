namespace Sencilla.Core.Serialization.Json;

[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonArrayAttribute : JsonConverterAttribute
{
    public JsonArrayAttribute(Type enumType): base(typeof(JsonArrayConverter<>).MakeGenericType(enumType))
    {
        if (!enumType.IsEnum)
            throw new ArgumentException(nameof(enumType));
    }
}
