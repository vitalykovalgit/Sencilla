
namespace Sencilla.Core;

[AttributeUsage(AttributeTargets.Property)]
public class JsonObjectStringAttribute : JsonConverterAttribute
{
    public JsonObjectStringAttribute() : base(typeof(JsonObjectStringConverter)) { }
}