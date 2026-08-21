namespace Sencilla.Component.I18n;

/// <summary>
/// Where a translation's current value came from. Decides what an automated run is allowed to
/// overwrite: a machine pass may replace its own earlier output, never a person's edit.
/// </summary>
public enum TranslationOrigin
{
    /// <summary>Seeded or imported — provenance was never recorded. Treated as machine-owned.</summary>
    Unknown = 0,

    /// <summary>Written by a translation provider run.</summary>
    Machine = 1,

    /// <summary>Written or corrected by a person in the admin. Never overwritten by a run.</summary>
    Human = 2
}

/// <summary>
/// One resource's value in one language, with the provenance that makes an automated re-run safe.
/// </summary>
[CrudApi("api/v1/i18n/translations")]
public class Translation : IEntity, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public int Id { get; set; }

    public string ResourceId { get; set; } = default!;

    public int LanguageId { get; set; }

    public string Value { get; set; } = default!;

    /// <summary>
    /// The language this value was translated FROM, so a re-run can reproduce the same pass and a
    /// reviewer can see what the translator actually read. Null on rows written before provenance
    /// existed.
    /// </summary>
    public int? SourceLanguageId { get; set; }

    /// <summary>
    /// Hash of the source text this value was produced from — see <see cref="TranslationHash"/>.
    ///
    /// Staleness is DERIVED from it rather than stored as a flag: editing a source string cannot
    /// then forget to mark its translations, and nothing has to fan out a write across every
    /// language when one source changes. A row is stale exactly when this does not match the hash
    /// of the source text as it reads today.
    /// </summary>
    public string? SourceHash { get; set; }

    /// <summary>Who last wrote <see cref="Value"/>. A run skips <see cref="TranslationOrigin.Human"/>.</summary>
    public TranslationOrigin Origin { get; set; }

    /// <summary>When <see cref="Value"/> was last written. Null on rows that predate provenance.</summary>
    public DateTime? UpdatedDate { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public Resource? Resource { get; set; }

    [ForeignKey(nameof(ResourceId))]
    public ResourceView? ResourceView { get; set; }
}

public class TranslationFilter : Filter<Translation>
{
    public TranslationFilter ByResourceId(params string[] resourceId)
    {
        AddProperty(nameof(Translation.ResourceId), typeof(string), resourceId.Cast<object>().ToArray());
        return this;
    }

    public TranslationFilter ByLanguageId(int languageId)
    {
        AddProperty(nameof(Translation.LanguageId), typeof(int), languageId);
        return this;
    }

    public TranslationFilter ByOrigin(params TranslationOrigin[] origin)
    {
        AddProperty(nameof(Translation.Origin), typeof(int), origin.Cast<object>().ToArray());
        return this;
    }
}
