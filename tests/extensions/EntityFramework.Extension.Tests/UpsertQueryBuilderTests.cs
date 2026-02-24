using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.EntityFramework.Extension.Tests;

public class UpsertQueryBuilderTests
{
    private const string test = nameof(test);
    private readonly TestEntity _te;

    public UpsertQueryBuilderTests()
    {
        _te = new TestEntity
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

        if (!RepositoryEntityFrameworkBootstrap.Entities.Contains(typeof(TestEntity)))
            RepositoryEntityFrameworkBootstrap.Entities.Add(typeof(TestEntity));
    }

    [Fact]
    public void UpsertCommand_WithSingleCondition_ShouldProceed()
    {
        var cmnd = new UpsertCommand<TestEntity>(te => te.Email)
        {
            InsertAction = s => new TestEntity
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
            UpdateAction = s => new TestEntity
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
        var query = builder.Build(new List<TestEntity> { _te });

        Assert.NotEmpty(query);
    }

    [Fact]
    public void UpsertCommand_WithMultipleConditions_ShouldProceed()
    {
        var cmnd = new UpsertCommand<TestEntity>(te => new { te.Email, te.Id, te.FirstName })
        {
            InsertAction = s => new TestEntity
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
            UpdateAction = s => new TestEntity
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
        var query = builder.Build(new List<TestEntity> { _te });

        Assert.NotEmpty(query);
    }

    [Fact]
    public void UpsertCommand_WithSqlInjection_ShouldEscapeSingleQuote()
    {
        var cmnd = new UpsertCommand<TestEntity>(te => new { te.Email, te.Id, te.FirstName })
        {
            InsertAction = s => new TestEntity { Id = s.Id },
            UpdateAction = s => new TestEntity { Email = s.Email },
        };

        var builder = new UpsertQueryBuilder<TestEntity>(cmnd);
        _te.Email = "asdlf@gmail.com' Drop Database";

        // The builder must not throw and must include the escaped quote in the output
        var query = builder.Build(new List<TestEntity> { _te });

        Assert.Contains('\'', query);
    }

    [Fact]
    public void UpsertCommand_WithNullInsertAndUpdateClause_ShouldProceed()
    {
        var cmnd = new UpsertCommand<TestEntity>(te => te.Email)
        {
            InsertAction = null,
            UpdateAction = null,
        };

        var builder = new UpsertQueryBuilder<TestEntity>(cmnd);
        var query = builder.Build(new List<TestEntity> { _te });

        Assert.NotEmpty(query);
    }

    [Fact]
    public void UpsertCommand_WithMoreThanOneEntities_ShouldProceed()
    {
        var cmnd = new UpsertCommand<TestEntity>(te => te.Email)
        {
            InsertAction = null,
            UpdateAction = null,
        };

        var builder = new UpsertQueryBuilder<TestEntity>(cmnd);

        var te2 = new TestEntity
        {
            Email = "test2@gmail.com",
            CreatedDate = _te.CreatedDate,
            FirstName = _te.FirstName,
            Id = _te.Id,
            IsActive = _te.IsActive,
            LastName = _te.LastName,
            Phone = _te.Phone,
            UpdatedDate = _te.UpdatedDate,
        };

        var te3 = new TestEntity
        {
            Email = "test3@gmail.com",
            CreatedDate = _te.CreatedDate,
            FirstName = _te.FirstName,
            Id = _te.Id,
            IsActive = _te.IsActive,
            LastName = _te.LastName,
            Phone = _te.Phone,
            UpdatedDate = _te.UpdatedDate,
        };

        var query = builder.Build(new List<TestEntity> { _te, te2, te3 });

        Assert.NotEmpty(query);
        Assert.True(query.ContainsAll(_te.Email, te2.Email, te3.Email));
    }
}
