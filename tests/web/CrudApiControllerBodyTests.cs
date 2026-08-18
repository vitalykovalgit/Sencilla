using Microsoft.AspNetCore.Mvc;

namespace Sencilla.Web.Tests;

/// <summary>
/// A body that does not match the action's parameter shape must be a 400, not a 500.
///
/// <see cref="CrudApiController{TEntity,TKey}"/> is deliberately not <c>[ApiController]</c> — that attribute
/// infers complex parameters as <c>[FromBody]</c> and would bind <c>Filter&lt;TEntity&gt;</c> from the body
/// instead of the query string, breaking every read endpoint. So ASP.NET does not produce the automatic 400
/// and the parameter simply arrives null. Before the guards, the first thing to touch it was
/// <c>entities.AsQueryable()</c> deep in the repository, which threw ArgumentNullException and surfaced as a
/// 500 — the reported symptom when a json object was posted to a collection route.
/// </summary>
public class CrudApiControllerBodyTests
{
    [CrudApi("api/v1/things")]
    public class Thing : IEntity<int>
    {
        public int Id { get; set; }
    }

    /// <summary>An empty provider: the guard must fire BEFORE any repository is resolved.</summary>
    private static CrudApiController<Thing, int> Controller()
        => new(new ServiceCollection().BuildServiceProvider());

    public static TheoryData<string, Func<CrudApiController<Thing, int>, Task>> NullBodyActions() => new()
    {
        { "CreateMany",     c => c.CreateMany(null!, default) },
        { "UpdateMany",     c => c.UpdateMany(null!, default) },
        { "UpsertMany",     c => c.UpsertMany(null!, default) },
        { "MergeMany",      c => c.MergeMany(null!, default) },
        { "Remove",         c => c.Remove(null!, default) },
        { "Undo",           c => c.Undo(null!, default) },
        { "Delete",         c => c.Delete((IEnumerable<Thing>)null!, default) },
        { "DeleteByIds",    c => c.DeleteByIds(null!, default) },
        { "CreateOne",      c => c.CreateOne(1, null!, default) },
        { "UpdateOne",      c => c.UpdateOne(1, null!, default) },
        { "UpsertOne",      c => c.UpsertOne(1, null!, default) },
        { "MergeOne",       c => c.MergeOne(1, null!, default) },
        { "CancelPending",  c => c.CancelPending(null!, default) },
    };

    [Theory]
    [MemberData(nameof(NullBodyActions))]
    public async Task UnbindableBody_IsABadRequest_NotAnUnhandledNullReference(
        string action, Func<CrudApiController<Thing, int>, Task> call)
    {
        var error = await Assert.ThrowsAsync<BadRequestException>(() => call(Controller()));

        Assert.Contains("Request body", error.Message);
        Assert.False(string.IsNullOrWhiteSpace(action));
    }

    /// <summary>
    /// BadRequestException is what the central handler maps to 400 — the guard is only useful if that
    /// mapping holds.
    /// </summary>
    [Fact]
    public async Task TheGuardsExceptionMapsTo400()
    {
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider() };
        await new SencillaExceptionHandler().TryHandleAsync(context, new BadRequestException("nope"), default);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    /// <summary>The guard must not disturb a well-formed body: an empty array is a legitimate no-op.</summary>
    [Fact]
    public async Task EmptyArray_IsNotTreatedAsMissing()
    {
        // No repository is registered, so a body that passes the guard falls through to 501 Not Implemented
        // (ApiController.NotImplemented uses the message-carrying overload, hence ObjectResult).
        var result = await Controller().UpdateMany([], default);

        Assert.Equal(StatusCodes.Status501NotImplemented, Assert.IsType<ObjectResult>(result).StatusCode);
    }
}
