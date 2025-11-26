namespace Microsoft.AspNetCore.Http;

[DisableInjection]
internal static class HttpHeaderExt
{

    public static long GetLong(this IHeaderDictionary headers, string header, long defaultValue)
    {
        if (headers == null) return defaultValue;
        if (string.IsNullOrWhiteSpace(header)) return defaultValue;
        if (!headers.TryGetValue(header, out var value)) return defaultValue;
        if (!long.TryParse(value, out long result)) return defaultValue;
        return result;
    }

    public static IDictionary<string, string>? GetMetadata(this IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(FileHeaders.UploadMetadata, out var metadata)) return null;

        var tuples = metadata.ToString().Split(',').Select(x =>
        {
            var kv = x.Split(' ');
            return (
                key: kv[0].ToLower(), 
                value: Encoding.UTF8.GetString(Convert.FromBase64String(kv[1]))
            );
        });
        
        return tuples?.ToDictionary(kv => kv.key, kv => kv.value);
    }

    public static Guid GetFileId(this PathString path) 
    {
        var segments = path.Value?.Split('/');
        if ((segments?.Length ?? 0) == 0)
            return Guid.Empty;

        return Guid.Parse(segments![segments.Length - 1]);
    }
}
