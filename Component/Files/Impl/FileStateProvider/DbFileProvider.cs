namespace Sencilla.Component.Files;

public class DbFileProvider : IFileProvider
{
    private readonly ICreateRepository<File, Guid> _createRepository;
    private readonly IUpdateRepository<File, Guid> _updateRepository;
    private readonly IReadRepository<File, Guid> _readRepository;
    private readonly IDeleteRepository<File, Guid> _deleteRepository;

    public DbFileProvider(
        ICreateRepository<File, Guid> createRepository,
        IUpdateRepository<File, Guid> updateRepository,
        IReadRepository<File, Guid> readRepository,
        IDeleteRepository<File, Guid> deleteRepository)
    {
        _createRepository = createRepository;
        _updateRepository = updateRepository;
        _readRepository = readRepository;
        _deleteRepository = deleteRepository;
    }

    public Task<File> CreateFile(File file) => _createRepository.Create(file);

    public Task<File> GetFile(Guid fileId) => _readRepository.GetById(fileId);

    public Task<File> UpdateFile(File file) => _updateRepository.Update(file);
}
