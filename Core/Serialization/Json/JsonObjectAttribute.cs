
namespace Sencilla.Core;

[AttributeUsage(AttributeTargets.Property)]
public class JsonObjectAttribute : JsonConverterAttribute
{
    public JsonObjectAttribute() : base(typeof(JsonObjectConverter)) { }

    //public override JsonConverter? CreateConverter(Type typeToConvert)
    //{
    //    return new JsonToObjectConverter();
    //}
}