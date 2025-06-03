namespace Sencilla.EntityFramework.Extension.Tests;

public class MergeQueryBuilderTests
{
    private const string test = nameof(test);
    private DbContextOptions<DynamicDbContext> _dbOptions;
    private RepositoryRegistrator _repoRegistrator = new();

    private TestEntity _te = new TestEntity()
    {
        Id = Guid.NewGuid(),
        Phone = 123456789,
        Email = "email@gmail.com",
        FirstName = test,
        LastName = test,
        IsActive = test != null,
        CreatedDate = DateTime.Now,
        UpdatedDate = DateTime.Now,
    };

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<DynamicDbContext>()
             .UseInMemoryDatabase(databaseName: "Picassa")
             .Options;

        _repoRegistrator.Entities.Add(typeof(TestEntity));
    }

    [Test]
    public void MergeCommand_WithSingleCondition_ShouldProceed()
    {
        var cmnd = new MergeCommand<TestEntity>(te => te.Id);

        var builder = new MergeQueryBuilder<TestEntity>(cmnd);

        var query = builder.Build(new List<TestEntity> { _te });

        Assert.That(query, Is.Not.Empty);
    }

    [Test]
    public void MergeCommand_WithEmptyEntities_ShouldProceedDelete()
    {
        var cmnd = new MergeCommand<TestEntity>(te => te.Id);

        var builder = new MergeQueryBuilder<TestEntity>(cmnd);

        var query = builder.Build(new List<TestEntity>(0));

        Assert.That(query, Is.Not.Empty);
    }
}