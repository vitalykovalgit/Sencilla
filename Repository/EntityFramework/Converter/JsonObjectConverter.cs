
namespace Sencilla.Repository.EntityFramework;

[DisableInjection]
public class JsonObjectConverter<T> : ValueConverter<T, string>
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public JsonObjectConverter()
        : base(
            obj => JsonSerializer.Serialize(obj, _options),
            json => JsonSerializer.Deserialize<T>(json, _options)!) { }

    /// <summary>
    /// Factory method for creating converter in runtime
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static ValueConverter CreateConverter(Type type) =>
        (ValueConverter)Activator.CreateInstance(typeof(JsonObjectConverter<>).MakeGenericType(type))!;
}
