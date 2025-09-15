
namespace System;

public static class SystemStringEx
{
    static JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static T? DeserializeTo<T>(this string? obj, JsonSerializerOptions? options = null)
    {
        return obj == null ? default : JsonSerializer.Deserialize<T>(obj, options ?? DefaultOptions);
    }
}
