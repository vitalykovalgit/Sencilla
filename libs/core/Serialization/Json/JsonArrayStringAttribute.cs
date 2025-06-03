namespace Sencilla.Core;

[AttributeUsage(AttributeTargets.Property)]
public class JsonArrayStringAttribute : JsonConverterAttribute
{
    public JsonArrayStringAttribute() : base(typeof(JsonArrayStringConverter)) { }
}