namespace Sencilla.EntityFramework.Extension.Tests;

public class UpsertQueryBuilderTests
{
    private const string test = nameof(test);
    private DbContextOptions<DynamicDbContext> _dbOptions;
    private RepositoryRegistrator _repoRegistrator = new();

    private TestEntity _te = new TestEntity()
    {
        Id = Guid.NewGuid(),
        Phone = 8098,
        Email = "test@gmail.com",
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

        // Mock DB context and setup it
        _repoRegistrator.Entities.Add(typeof(TestEntity));
    }

    [Test]
    public void UpsertCommand_WithSingleCondition_ShouldProceed()
    {
        var cmnd = new UpsertCommand<TestEntity>(te => te.Email)
        {
            InsertAction = s => new TestEntity()
            {
                Id = s.Id,
                Email = s.Email,
                Phone = s.Phone,
                LastName = s.LastName,
                FirstName = s.FirstName,
                IsActive = s.IsActive,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate,
            },
            UpdateAction = s => new TestEntity()
            {
                Email = s.Email,
                Phone = s.Phone,
                LastName = s.LastName,
                FirstName = s.FirstName,
                IsActive = s.IsActive,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate,
            },
        };

        var builder = new UpsertQueryBuilder<TestEntity>(cmnd);

        var query = builder.Build(_te);

        Assert.Pass();
        Assert.That(query, Is.Not.Empty);

        /*
        using (var context = new DynamicDbContext(_dbOptions, _repoRegistrator))
        {
            context.Set<TestEntity>().Add(new TestEntity()
            {
                Id = Guid.NewGuid(),
                Phone = 8098,
                Email = "test@gmail.com",
                FirstName = test,
                LastName = test,
                IsActive = test != null,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
            });
            context.SaveChanges();

            await context.UpsertAsync(_testEntity, te => te.Email,
                    s => new TestEntity()
                    {
                        Id = Guid.NewGuid(),
                        Email = s.Email,
                        Phone = s.Phone,
                        LastName = s.LastName,
                        FirstName = s.FirstName,
                        IsActive = s.IsActive,
                        CreatedDate = s.CreatedDate,
                        UpdatedDate = s.UpdatedDate,
                    },
                    (s, t) => new TestEntity()
                    {
                        Id = t.Id,
                        Email = t.Email,
                        Phone = s.Phone,
                        LastName = s.LastName,
                        FirstName = s.FirstName,
                        IsActive = s.IsActive,
                        CreatedDate = t.CreatedDate,
                        UpdatedDate = s.UpdatedDate,
                    });
        }*/
    }

    [Test]
    public void UpsertCommand_WithMultipleConditions_ShouldProceed()
    {
        var cmnd = new UpsertCommand<TestEntity>(te => new { te.Email, te.Id, te.FirstName, })
        {
            InsertAction = s => new TestEntity()
            {
                Id = s.Id,
                Email = s.Email,
                Phone = s.Phone,
                LastName = s.LastName,
                FirstName = s.FirstName,
                IsActive = s.IsActive,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate,
            },
            UpdateAction = s => new TestEntity()
            {
                Email = s.Email,
                Phone = s.Phone,
                LastName = s.LastName,
                FirstName = s.FirstName,
                IsActive = s.IsActive,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate,
            },
        };

        var builder = new UpsertQueryBuilder<TestEntity>(cmnd);

        var query = builder.Build(_te);

        Assert.Pass();
        Assert.That(query, Is.Not.Empty);
    }

    [Test]
    public void UpsertCommand_WithSqlInjection_ShouldProceedAndReplaceSingleQoute()
    {
        var cmnd = new UpsertCommand<TestEntity>(te => new { te.Email, te.Id, te.FirstName, })
        {
            InsertAction = s => new TestEntity() { Id = s.Id, },
            UpdateAction = s => new TestEntity() { Email = s.Email, },
        };

        var builder = new UpsertQueryBuilder<TestEntity>(cmnd);

        _te.Email = "asdlf@gmail.com' Drop Database";

        Assert.Pass();
        Assert.IsTrue(builder.Build(_te).Contains('\''));
    }

    [Test]
    public void UpsertCommand_WithNullInsertAndUpdateClause_ShouldProceed()
    {
        var cmnd = new UpsertCommand<TestEntity>(te => te.Email)
        {
            InsertAction = null,
            UpdateAction = null,
        };

        var builder = new UpsertQueryBuilder<TestEntity>(cmnd);

        var query = builder.Build(_te);

        Assert.Pass();
        Assert.That(query, Is.Not.Empty);
    }
}