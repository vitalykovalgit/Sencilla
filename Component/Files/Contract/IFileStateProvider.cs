namespace Sencilla.Component.Files;

// TODO: Think about use different model for file state
//       Think about file id type
public interface IFileStateProvider
{
    Task<File> GetFileState(string fileId = null);
    Task<File> SetFileState(File fileState);
}
