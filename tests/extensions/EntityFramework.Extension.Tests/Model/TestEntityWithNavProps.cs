using Sencilla.Core;

namespace Sencilla.EntityFramework.Extension.Tests.Model;

[Table(nameof(TestEntityWithNavProps), Schema = "test")]
internal class TestEntityWithNavProps : IEntity<int>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int ChildId { get; set; }

    // Reference navigation — should be excluded from SQL
    public TestChildEntity? Child { get; set; }

    // Collection navigation — should be excluded from SQL
    public ICollection<TestChildEntity>? Children { get; set; }

    // NotMapped — should be excluded from SQL
    [NotMapped]
    public string? Computed { get; set; }
}

[Table(nameof(TestChildEntity), Schema = "test")]
internal class TestChildEntity : IEntity<int>
{
    public int Id { get; set; }
    public string? Label { get; set; }
}
