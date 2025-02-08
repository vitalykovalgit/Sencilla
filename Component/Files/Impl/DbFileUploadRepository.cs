namespace Sencilla.Component.Files;

public class DbFileUploadRepository : IFileUploadRepository
{
    private readonly ICreateRepository<FileUpload, Guid> _createRepository;
    private readonly IUpdateRepository<FileUpload, Guid> _updateRepository;
    private readonly IReadRepository<FileUpload, Guid> _readRepository;
    private readonly IDeleteRepository<FileUpload, Guid> _deleteRepository;

    public DbFileUploadRepository(
        ICreateRepository<FileUpload, Guid> createRepository,
        IUpdateRepository<FileUpload, Guid> updateRepository,
        IReadRepository<FileUpload, Guid> readRepository,
        IDeleteRepository<FileUpload, Guid> deleteRepository)
    {
        _createRepository = createRepository;
        _updateRepository = updateRepository;
        _readRepository = readRepository;
        _deleteRepository = deleteRepository;
    }

    public Task<FileUpload> CreateFileUpload(FileUpload fileUpload) => _createRepository.Create(fileUpload);

    public Task<FileUpload> GetFileUpload(Guid fileId) => _readRepository.GetById(fileId);

    public Task<FileUpload> UpdateFileUpload(FileUpload fileUpload) => _updateRepository.Update(fileUpload);
}
