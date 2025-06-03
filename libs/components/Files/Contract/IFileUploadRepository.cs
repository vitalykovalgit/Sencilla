namespace Sencilla.Component.Files;

public interface IFileUploadRepository
{
    Task<FileUpload?> CreateFileUpload(FileUpload fileUpload);
    Task<FileUpload?> GetFileUpload(Guid fileUploadId);
    Task<FileUpload?> UpdateFileUpload(FileUpload fileUpload);
    Task<int> DeleteFileUpload(Guid fileUploadId);
}
