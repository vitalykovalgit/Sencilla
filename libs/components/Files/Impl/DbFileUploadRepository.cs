namespace Sencilla.Component.Files;

public class DbFileUploadRepository(
    ICreateRepository<FileUpload, Guid> createRepository,
    IUpdateRepository<FileUpload, Guid> updateRepository,
    IReadRepository<FileUpload, Guid> readRepository,
    IDeleteRepository<FileUpload, Guid> deleteRepository)
    : IFileUploadRepository
{
    public Task<FileUpload?> CreateFileUpload(FileUpload fileUpload) => createRepository.Create(fileUpload);

    public Task<FileUpload?> GetFileUpload(Guid fileUploadId) => readRepository.GetById(fileUploadId);

    public Task<FileUpload?> UpdateFileUpload(FileUpload fileUpload) => updateRepository.Update(fileUpload);

    public Task<int> DeleteFileUpload(Guid fileUploadId) => deleteRepository.Delete(fileUploadId);
}
