using Sencilla.Core;

namespace Sencilla.EntityFramework.Extension.Tests.Model;

/// <summary>
/// Mirrors Sencilla.Component.I18n's `Resource`: a client-supplied string primary key plus a
/// collection navigation. Both traits broke upsert, so the shape is worth a permanent fixture.
/// </summary>
[Table(nameof(StringKeyEntity), Schema = "test")]
internal class StringKeyEntity : IEntity<string>
{
    public string Id { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestChildEntity>? Children { get; set; }
}
