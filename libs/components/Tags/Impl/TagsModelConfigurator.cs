namespace Sencilla.Component.Tags;

/// <summary>
/// Teaches <c>DynamicDbContext</c> what the taggable marker interfaces mean, from THIS component rather than
/// from the context: the shared context knows only <see cref="IEntityModelConfigurator"/>, so tagging can be
/// added, changed or removed without touching the repository library (open/closed), and the rule for a tag
/// column lives next to the code that reads and writes it.
///
/// <para>Discovered and registered automatically by the <c>AddSencilla()</c> scan; <c>AddSencillaTags()</c>
/// registers it too, idempotently, for hosts that skip the scan.</para>
/// </summary>
public class TagsModelConfigurator : IEntityModelConfigurator
{
    /// <summary>
    /// <see cref="IEntityTaggable.Tags"/> is the wire contract for all three tag repositories, so EF's own
    /// conventions see a <c>List&lt;string&gt;</c> on every taggable entity and map it as a primitive collection
    /// (a JSON array column) — the context's reflection mapper skips it, classifying every non-string class
    /// property as a navigation, but conventions run regardless. What that leaves is one decision per storage
    /// strategy:
    /// <list type="bullet">
    /// <item><b>Inline</b> — that column IS the storage, so bound it: without a max length EF sends
    /// <c>nvarchar(-1)</c> parameters at the <c>NVARCHAR(4000)</c> column the dacpac declares.</item>
    /// <item><b>Linked / Shared</b> — the rows live in a side table, so the phantom column must not exist.</item>
    /// </list>
    /// </summary>
    public void Configure(EntityTypeBuilder builder, Type entityType)
    {
        if (typeof(IEntityTaggableInline).IsAssignableFrom(entityType))
            builder.Property(nameof(IEntityTaggable.Tags)).HasMaxLength(TagName.MaxSetLength);
        else if (typeof(IEntityTaggable).IsAssignableFrom(entityType))
            builder.Ignore(nameof(IEntityTaggable.Tags));
    }
}
