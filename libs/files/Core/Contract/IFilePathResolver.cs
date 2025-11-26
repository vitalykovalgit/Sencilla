namespace Sencilla.Component.Files;

public interface IFilePathResolver
{
    string GetFullPath(File file);
}
