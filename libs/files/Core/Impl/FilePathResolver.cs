namespace Sencilla.Component.Files;

[DisableInjection]
internal class FilePathResolver : IFilePathResolver
{
    private static bool[]? _lookup;

    public FilePathResolver() 
    {
        if (_lookup == null)
        {
            _lookup = new bool[65536];
            for (char c = '0'; c <= '9'; c++) _lookup[c] = true;
            for (char c = 'A'; c <= 'Z'; c++) _lookup[c] = true;
            for (char c = 'a'; c <= 'z'; c++) _lookup[c] = true;
            //_lookup['.'] = true;
            _lookup['_'] = true;
            _lookup['-'] = true;
        }

    }

    public string GetFullPath(File file)
    {
        var fileDim = file.Dim == null ? "" : $"_{file.Dim}px";
        var fileName = $"{file.Id}{fileDim}{Path.GetExtension(file.Name)}";
        
        var projectId = file.Attrs?.GetString("projectId");
        var projectPath = projectId == null ? "" : $"project{projectId}";

     
        return file.Origin switch
        {
            FileOrigin.User => Path.Combine($"user{file.UserId ?? 0}", projectPath, fileName),
            FileOrigin.System => $"system/{fileName}",
            _ => $"none/{fileName}"
        };
    }

    public string GetResolutionPath(File file, int res)
    {
        var path = file.Path ?? GetFullPath(file);
        var ext = Path.GetExtension(path);
        var pathWithoutExt = path[..^ext.Length];
        return $"{pathWithoutExt}_{res}{ext}";
    }

    public static string RemoveSpecialCharacters(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        char[] buffer = new char[str.Length];
        int index = 0;
        foreach (char c in str)
        {
            if (_lookup![c])
            {
                buffer[index] = c;
                index++;
            }
        }
        return new string(buffer, 0, index);
    }
}
