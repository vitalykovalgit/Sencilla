namespace Sencilla.Component.Tags;

/// <summary>
/// Fills <c>Tags</c> on rows read from a side-table repository, so every taggable entity answers with the same
/// <c>tags: string[]</c> wire shape regardless of where its tags live. Registered only for the side-table
/// repositories — the inline column arrives with the row, and an entity with no handler costs no dispatch.
///
/// <para>OPT-IN, on <c>?with=tags</c> — the framework's existing vocabulary for "load this too", so a list read
/// costs its second query only when the caller wants tags. It cannot be a real EF <c>Include</c>: for inline
/// storage <c>Tags</c> is a primitive collection, for the other two it is ignored by the model, and the shared
/// table has no foreign key to navigate. <c>FilterConstraintHandler</c> therefore drops <c>with=tags</c> as a
/// non-navigation, and this handler is what gives it meaning.</para>
///
/// <para>ponytail: list reads only. <c>IReadRepository.GetById</c> takes no <see cref="IFilter"/>, so a
/// single-row GET cannot carry <c>?with=</c> and reads its tags from <c>{route}/{id}/tags</c> instead. Give
/// <c>GetById</c> a filter if that asymmetry ever matters.</para>
/// </summary>
public class TagHydrationHandler<TEntity, TKey>(ITagRepository<TEntity, TKey> repository) : IEventHandler<EntityReadEvent<TEntity>>
    where TEntity : class, IEntity<TKey>, IEntityTaggable
{
    public Task HandleAsync(EntityReadEvent<TEntity> @event, CancellationToken token)
        => Wanted(@event.Filter) ? repository.Hydrate(@event.Entities, token) : Task.CompletedTask;

    /// <summary>
    /// Matched case-insensitively against the entity's own property name, so <c>?with=tags</c> and
    /// <c>?with=Tags</c> behave alike — as they do for every other <c>with</c> value.
    /// </summary>
    static bool Wanted(IFilter? filter)
        => filter?.With?.Contains(nameof(IEntityTaggable.Tags), StringComparer.OrdinalIgnoreCase) == true;
}
