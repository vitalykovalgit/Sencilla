namespace Sencilla.Component.Files;

public interface IFileUploadRepository
{
    Task<FileUpload> CreateFileUpload(FileUpload fileUpload);
    Task<FileUpload> GetFileUpload(Guid fileId);
    Task<FileUpload> UpdateFileUpload(FileUpload fileUpload);
}
