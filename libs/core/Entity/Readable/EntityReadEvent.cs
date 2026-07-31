
namespace Sencilla.Core;

/// <summary>
/// Fired AFTER a read has materialised, completing the -ing/-ed pair that create, update and delete already
/// have. <see cref="EntityReadingEvent{TEntity}"/> shapes the query; this one sees the rows.
///
/// <para>Deliberately collection-shaped: a handler that needs a second query (hydrating tags, permissions,
/// derived fields) can serve a whole page with one round trip instead of N+1. <c>GetById</c> publishes a
/// single-element list.</para>
///
/// <para>Handlers may mutate the entities in place; they must not replace the list.</para>
/// </summary>
public class EntityReadEvent<TEntity> : Event
{
    /// <summary>The filter the read ran with, or null.</summary>
    public IFilter? Filter { get; set; }

    /// <summary>The rows that were read.</summary>
    public IList<TEntity> Entities { get; set; } = [];
}
