namespace Sencilla.Component.Files;

// TODO: Think about use different model for file state
//       Think about file id type
public interface IFileProvider
{
    Task<File> CreateFile(File file);
    Task<File> GetFile(Guid fileId);
    Task<File> UpdateFile(File file);
}
