namespace Sencilla.Core.Serialization.Json
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class JsonArrayEnumAttribute : JsonConverterAttribute
    {
        public JsonArrayEnumAttribute(Type enumType): base(typeof(JsonArrayEnumConverter<>).MakeGenericType(enumType))
        {
            if (!enumType.IsEnum)
                throw new ArgumentException(nameof(enumType));
        }
    }
}
