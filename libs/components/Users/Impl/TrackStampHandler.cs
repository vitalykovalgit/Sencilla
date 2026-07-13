namespace Sencilla.Component.Users;

/// <summary>
/// Stamps audit attribution (CreatedBy/UpdatedBy) from the current user in the
/// write pipeline. Client-supplied values are always overwritten — attribution is
/// a framework fact, not client input. Null user (anonymous/system) stamps null.
/// Registered per entity type by <see cref="TrackStampRegistrator"/>.
/// </summary>
public class TrackStampHandler<TEntity>
    : IEventHandlerBase<EntityCreatingEvent<TEntity>>
    , IEventHandlerBase<EntityUpdatingEvent<TEntity>>
{
    public Task HandleAsync(EntityCreatingEvent<TEntity> @event, ISystemVariable sysVars, CancellationToken token)
    {
        var userId = CurrentUserId(sysVars);
        foreach (var e in @event?.Entities ?? Enumerable.Empty<TEntity>().AsQueryable())
        {
            if (e is IEntityCreatedByTrack created)
                created.CreatedBy = userId;

            // Creation is the first modification — never persist a forged value.
            if (e is IEntityUpdatedByTrack updated)
                updated.UpdatedBy = userId;
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(EntityUpdatingEvent<TEntity> @event, ISystemVariable sysVars, CancellationToken token)
    {
        var userId = CurrentUserId(sysVars);
        foreach (var e in @event?.Entities ?? Enumerable.Empty<TEntity>().AsQueryable())
        {
            if (e is IEntityUpdatedByTrack updated)
                updated.UpdatedBy = userId;
        }

        return Task.CompletedTask;
    }

    private static Guid? CurrentUserId(ISystemVariable sysVars)
    {
        var user = sysVars.GetCurrentUser();
        return user == null || user.Id == Guid.Empty ? null : user.Id;
    }
}

/// <summary>
/// Registers <see cref="TrackStampHandler{TEntity}"/> for every entity that opts
/// into CreatedBy/UpdatedBy attribution.
/// </summary>
public class TrackStampRegistrator : ITypeRegistrator
{
    public void Register(IServiceCollection container, Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false, IsGenericType: false })
            return;

        var createdBy = type.IsAssignableTo(typeof(IEntityCreatedByTrack));
        var updatedBy = type.IsAssignableTo(typeof(IEntityUpdatedByTrack));
        if (!createdBy && !updatedBy)
            return;

        var handler = typeof(TrackStampHandler<>).MakeGenericType(type);

        if (createdBy || updatedBy)
            container.AddTransient(typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityCreatingEvent<>).MakeGenericType(type)), handler);

        if (updatedBy)
            container.AddTransient(typeof(IEventHandlerBase<>).MakeGenericType(typeof(EntityUpdatingEvent<>).MakeGenericType(type)), handler);
    }
}
