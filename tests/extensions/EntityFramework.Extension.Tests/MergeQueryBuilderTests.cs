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
    /// This suite only ever exercised the navigation-free TestEntity and asserted Assert.NotEmpty,
    /// which is why MergeQueryBuilder kept its own [NotMapped]-only column filter while the other two
    /// builders moved to the shared rule — and why merging any entity with a navigation threw
    /// «Invalid column name».
    [Fact]
    public void Build_ExcludesNavigationAndNotMappedColumns()
    {
        if (!RepositoryEntityFrameworkBootstrap.Entities.Contains(typeof(TestEntityWithNavProps)))
            RepositoryEntityFrameworkBootstrap.Entities.Add(typeof(TestEntityWithNavProps));

        var entity = new TestEntityWithNavProps
        {
            Id = 1,
            Name = test,
            ChildId = 2,
            Child = new TestChildEntity { Id = 2, Label = test },
            Children = [new TestChildEntity { Id = 3, Label = test }],
            Computed = test,
        };

        var query = new MergeQueryBuilder<TestEntityWithNavProps>(new MergeCommand<TestEntityWithNavProps>(e => e.Id))
            .Build([entity]);

        Assert.DoesNotContain("[Children]", query);
        Assert.DoesNotContain("[Child]", query);
        Assert.DoesNotContain("[Computed]", query);
        Assert.Contains("[Name]", query);
    }
}
