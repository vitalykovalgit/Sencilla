namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Extra model configuration for every entity <see cref="DynamicDbContext"/> maps — the seam through which a
/// component teaches the shared context about its own marker interfaces WITHOUT the context knowing that the
/// component exists.
///
/// <para>Implementations are resolved from DI and applied last, after the reflection mapper and the built-in
/// marker hooks, so a configurator can override a convention. Discovery is automatic: a public implementation
/// in a scanned assembly is registered by <c>AddSencilla()</c> like any other service.</para>
///
/// <example>
/// The tags component's configurator, which is why this interface exists:
/// <code>
/// public class TagsModelConfigurator : IEntityModelConfigurator
/// {
///     public void Configure(EntityTypeBuilder builder, Type entityType)
///     {
///         if (typeof(IEntityTaggableInline).IsAssignableFrom(entityType))
///             builder.Property(nameof(IEntityTaggable.Tags)).HasMaxLength(TagName.MaxSetLength);
///         else if (typeof(IEntityTaggable).IsAssignableFrom(entityType))
///             builder.Ignore(nameof(IEntityTaggable.Tags));
///     }
/// }
/// </code>
/// </example>
///
/// <para>Called once per entity type while the model is being built, and the model is built once per process —
/// so an implementation must be pure and must not touch the database.</para>
/// </summary>
public interface IEntityModelConfigurator
{
    /// <summary>
    /// Configures <paramref name="entityType"/>. Called for EVERY mapped entity, so an implementation is
    /// expected to test the type for its own marker interface and do nothing otherwise.
    /// </summary>
    void Configure(EntityTypeBuilder builder, Type entityType);
}
