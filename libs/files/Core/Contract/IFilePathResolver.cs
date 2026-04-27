namespace Sencilla.Component.Files;

public interface IFilePathResolver
{
    string GetFullPath(File file);
    string GetResolutionPath(File file, int res);
    string GetResolutionPath(string path, string resKey);
}
