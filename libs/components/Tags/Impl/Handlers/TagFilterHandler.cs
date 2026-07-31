namespace Sencilla.Component.Tags;

/// <summary>
/// Applies <c>IFilter.Tag</c> — <c>?tag=a&amp;tag=b</c> — to a taggable entity's read query, ANY semantics, the
/// same OR every other filter property uses. Composes into the reading pipeline beside the property filter and
/// the permission constraints, so tag filtering is automatically ANDed with them.
/// </summary>
public class TagFilterHandler<TEntity, TKey>(ITagRepository<TEntity, TKey> repository) : IEventHandler<EntityReadingEvent<TEntity>>
    where TEntity : class, IEntity<TKey>, IEntityTaggable
{
    public async Task HandleAsync(EntityReadingEvent<TEntity> @event, CancellationToken token)
    {
        var requested = @event.Filter?.Tag;
        if (requested == null || requested.Length == 0 || @event.Entities == null)
            return;

        var tags = TagName.TrySet(requested);

        // Every requested tag was malformed: match NOTHING. Returning the query untouched would silently widen
        // a deliberate filter to "everything", which is the worst possible reading of a bad tag.
        if (tags.Length == 0)
        {
            @event.Entities = @event.Entities.Where(_ => false);
            return;
        }

        @event.Entities = await repository.FilterAny(@event.Entities, tags, token);
    }
}
