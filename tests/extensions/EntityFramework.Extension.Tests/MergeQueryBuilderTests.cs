using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.EntityFramework.Extension.Tests;

public class MergeQueryBuilderTests
{
    private const string test = nameof(test);
    private readonly TestEntity _te;

    public MergeQueryBuilderTests()
    {
        _te = new TestEntity
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

        if (!RepositoryEntityFrameworkBootstrap.Entities.Contains(typeof(TestEntity)))
            RepositoryEntityFrameworkBootstrap.Entities.Add(typeof(TestEntity));
    }

    [Fact]
    public void MergeCommand_WithSingleCondition_ShouldProceed()
    {
        var cmnd = new MergeCommand<TestEntity>(te => te.Id);
        var builder = new MergeQueryBuilder<TestEntity>(cmnd);

        var query = builder.Build(new List<TestEntity> { _te });

        Assert.NotEmpty(query);
    }

    [Fact]
    public void MergeCommand_WithEmptyEntities_ShouldProceedDelete()
    {
        var cmnd = new MergeCommand<TestEntity>(te => te.Id);
        var builder = new MergeQueryBuilder<TestEntity>(cmnd);

        var query = builder.Build(new List<TestEntity>(0));

        Assert.NotEmpty(query);
    }
}
