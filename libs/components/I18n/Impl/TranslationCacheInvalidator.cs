namespace Sencilla.Component.I18n;

/// <summary>
/// Clears the localization cache the moment a translation is written, so the admin who saved a
/// string sees it on the next request instead of waiting out the TTL.
///
/// Best-effort by nature: it only reaches the process that handled the write. Multi-instance
/// deployments still depend on <see cref="CacheLocalizationProvider.DefaultTtl"/> — this is the fast
/// path, not the correctness guarantee.
/// </summary>
public class TranslationCacheInvalidator(ILocalizationProvider provider)
    : IEventHandlerBase<EntityCreatedEvent<Translation>>
    , IEventHandlerBase<EntityUpdatedEvent<Translation>>
    , IEventHandlerBase<EntityDeletedEvent<Translation>>
    , IEventHandlerBase<EntityUpdatedEvent<Resource>>
{
    public Task HandleAsync(EntityCreatedEvent<Translation> @event) => Clear();

    public Task HandleAsync(EntityUpdatedEvent<Translation> @event) => Clear();

    public Task HandleAsync(EntityDeletedEvent<Translation> @event) => Clear();

    /// <summary>A renamed resource changes what every language is a translation OF.</summary>
    public Task HandleAsync(EntityUpdatedEvent<Resource> @event) => Clear();

    private Task Clear()
    {
        if (provider is CacheLocalizationProvider cache)
            cache.Invalidate();

        return Task.CompletedTask;
    }
}
