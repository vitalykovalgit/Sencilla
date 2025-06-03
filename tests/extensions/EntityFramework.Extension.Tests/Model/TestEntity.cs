using Sencilla.Core;

namespace Sencilla.EntityFramework.Extension.Tests.Model;

[Table(nameof(TestEntity), Schema = "test")]
internal class TestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public long Phone { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
