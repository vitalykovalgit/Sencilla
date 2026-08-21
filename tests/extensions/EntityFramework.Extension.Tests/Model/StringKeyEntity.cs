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

    /// The shapes that fell through every type branch and emitted raw ToString(): a nullable date,
    /// an enum (member NAME, not its value) and a decimal (comma separator under uk-UA).
    public DateTime? UpdatedDate { get; set; }
    public SampleOrigin Origin { get; set; }
    public decimal? Price { get; set; }

    public ICollection<TestChildEntity>? Children { get; set; }
}

/// Mirrors Sencilla.Component.I18n's TranslationOrigin.
internal enum SampleOrigin
{
    Unknown = 0,
    Machine = 1,
    Human = 2
}
