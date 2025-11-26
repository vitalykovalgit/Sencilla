namespace System.Collections.Generic;

[DisableInjection]
internal static class MetadataHeaderExt
{

    public static long? GetLong(this IDictionary<string, string> metadata, string key, long? defaultValue = null)
    {
        if (metadata == null) return defaultValue;
        if (string.IsNullOrWhiteSpace(key)) return defaultValue;
        
        key = key.ToLower();
        if (!metadata.TryGetValue(key, out var value)) return defaultValue;
        if (!long.TryParse(value, out long result)) return defaultValue;
        
        return result;
    }

    public static int? GetInt(this IDictionary<string, string> metadata, string key, int? defaultValue = null)
    {
        if (metadata == null) return defaultValue;
        if (string.IsNullOrWhiteSpace(key)) return defaultValue;

        key = key.ToLower();
        if (!metadata.TryGetValue(key, out var value)) return defaultValue;
        if (!int.TryParse(value, out int result)) return defaultValue;
        
        return result;
    }

    public static Guid? GetGuid(this IDictionary<string, string> metadata, string key, Guid? defaultValue = null)
    {
        if (metadata == null) return defaultValue;
        if (string.IsNullOrWhiteSpace(key)) return defaultValue;

        key = key.ToLower();
        if (!metadata.TryGetValue(key, out var value)) return defaultValue;
        if (!Guid.TryParse(value, out Guid result)) return defaultValue;
        
        return result;
    }

    public static string? GetString(this IDictionary<string, string> metadata, string key, string? defaultValue = null)
    {
        if (metadata == null) return defaultValue;
        if (string.IsNullOrWhiteSpace(key)) return defaultValue;
        if (!metadata.TryGetValue(key.ToLower(), out var value)) return defaultValue;

        return value;
    }

    public static TEnum? GetEnum<TEnum>(this IDictionary<string, string> metadata, string key, TEnum? defaultValue = null) where TEnum : struct
    {
        if (metadata == null) return defaultValue;
        if (string.IsNullOrWhiteSpace(key)) return defaultValue;
        
        key = key.ToLower();
        if (!metadata.TryGetValue(key, out var value)) return defaultValue;
        if (!Enum.TryParse<TEnum>(value, out TEnum result)) return defaultValue;

        return result;
    }

    public static IDictionary<string, string> RemoveKeys(this IDictionary<string, string> metadata, params string[] keys)
    {
        keys.ForEach(key => metadata.Remove(key.ToLower()));
        return metadata;
    }

}
