namespace Sencilla.Component.FilesTus.Extension
{
    internal static class MetadataExt
    {
        //"filename cGV4ZWxzLWNvZHkta2luZy0xMTE4NjY3LmpwZw==,filetype aW1hZ2UvanBlZw=="
        public static IDictionary<string, string> ParseMetadataHeader(this string metadataHeader) =>
            metadataHeader.Split(',').Select(x =>
            {
                var kv = x.Split(' ');
                return (key: kv[0], value: kv[1]);
            }).ToImmutableDictionary(kv => kv.key, kv => kv.value.FromBase64());

        public static string FromBase64(this string b64) => Encoding.UTF8.GetString(Convert.FromBase64String(b64));

        public static string? MimeTypeExt(this string mimeType) => IFormFileEx.MimeTypeToExt[mimeType];
    }
}
