namespace Sencilla.Component.Tags;

/// <summary>
/// The tag endpoints for every <c>[CrudApi]</c> entity, mounted under that entity's own route
/// (<c>{route}/tags</c>, <c>{route}/{id}/tags</c>) by <see cref="EntityApiAttribute"/> — so tagging ships with
/// this component instead of living in the CRUD controller, and a host that doesn't reference it has no tag
/// routes at all.
///
/// <para>Deliberately constrained no more tightly than <see cref="ITagRepository{TEntity,TKey}"/>: the routes
/// exist for every entity and answer 501 when the entity isn't taggable, which is a better answer than binding
/// <c>tags</c> as an id and failing at 400.</para>
/// </summary>
[EntityApi]
public class TagApiController<TEntity, TKey>(IServiceProvider resolver) : ApiController(resolver)
    where TEntity : class, IEntity<TKey>, new()
{
    /// <summary>
    /// <c>GET {route}/tags</c> — every tag in use on this entity, for admin autocomplete. A literal route
    /// segment, so it wins over <c>{id}</c> (as <c>count</c>/<c>sum</c>/<c>min</c> already do). 501 when the
    /// entity is not <see cref="IEntityTaggable"/>.
    /// </summary>
    [HttpGet, Route("tags")]
    public virtual async Task<IActionResult> GetTags(CancellationToken token)
    {
        return await FromService((ITagRepository<TEntity, TKey> repository) => repository.Distinct(token));
    }

    /// <summary><c>GET {route}/{id}/tags</c> — one row's tags, normalised and ordinally sorted.</summary>
    [HttpGet, Route("{id}/tags")]
    public virtual async Task<IActionResult> GetTags(TKey id, CancellationToken token)
    {
        return await FromService((ITagRepository<TEntity, TKey> repository) => repository.Get(id, token));
    }

    /// <summary>
    /// <c>POST {route}/{id}/tags</c> with <c>["a","b"]</c> — REPLACES the row's whole tag set (<c>[]</c>
    /// clears it), and echoes back the normalised result. One call from a chips UI, no client-side diffing.
    ///
    /// <para>Authorisation comes from the TAGGED ROW: the row is loaded through its own read repository first,
    /// so an invisible row is a 404 and tag writes can never be a back door around the entity's permissions.
    /// Malformed tags surface as 400 from <c>TagName</c>.</para>
    /// </summary>
    [HttpPost, Route("{id}/tags")]
    public virtual async Task<IActionResult> SetTags(TKey id, [FromBody] string[]? tags, CancellationToken token)
    {
        var repository = R<ITagRepository<TEntity, TKey>>();
        if (repository is null)
            return NotImplemented();

        if (!await IsVisible(id, token))
            return NotFound();

        await repository.Set(id, tags ?? [], token);
        return Ok(await repository.Get(id, token));
    }

    /// <summary>
    /// <c>DELETE {route}/{id}/tags?tag=a&amp;tag=b</c> — removes the named tags, ignoring ones the row does not
    /// carry. Requires at least one <c>tag</c>: a bare DELETE is a 400, not "clear everything", so a client
    /// that dropped its query string cannot wipe a tag set by accident (clearing is <c>POST</c> with <c>[]</c>).
    /// </summary>
    [HttpDelete, Route("{id}/tags")]
    public virtual async Task<IActionResult> RemoveTags(TKey id, [FromQuery] string[]? tag, CancellationToken token)
    {
        var repository = R<ITagRepository<TEntity, TKey>>();
        if (repository is null)
            return NotImplemented();

        if (tag == null || tag.Length == 0)
            throw new BadRequestException("tag-required");

        if (!await IsVisible(id, token))
            return NotFound();

        await repository.Remove(id, tag, token);
        return Ok(await repository.Get(id, token));
    }

    /// <summary>
    /// Whether the caller can see this row AT ALL — the read pipeline applies the same permission constraints a
    /// GET would, so an unauthorised row reads back as null and is reported as missing (the house convention:
    /// foreign rows are invisible, not forbidden).
    /// </summary>
    private async Task<bool> IsVisible(TKey id, CancellationToken token)
    {
        var repo = R<IReadRepository<TEntity, TKey>>();

        return repo != null && await repo.GetById(id, token) != null;
    }
}
