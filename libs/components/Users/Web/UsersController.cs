
namespace Sencilla.Component.Users;

[Route("api/v1/users")]
public class UsersController(IServiceProvider resolver) : CrudApiController<User, Guid>(resolver)
{
    /// <summary>
    /// The caller's own account, found by its id — an account may have no email. Read through the repository,
    /// so the security matrix still decides what the caller may see of it.
    /// </summary>
    [HttpGet, Route("current")]
    public virtual async Task<IActionResult> GetCurrentUser(
        [FromServices] ICurrentUserProvider userProvider,
        [FromServices] IReadRepository<User, Guid> userReader, CancellationToken token)
    {
        var current = userProvider.CurrentUser;
        if (current.IsAnonymous())
            return NotFound();

        return Ok(await userReader.GetById(current.Id, token, with => with.Roles!));
    }
}
