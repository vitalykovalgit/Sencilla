
namespace Sencilla.Component.Users;

[Route("api/v1/users")]
public class UsersController : CrudApiController<User>
{
    public UsersController(IResolver resolver): base(resolver)
    {
    }

    [HttpGet, Route("current")]
    public virtual async Task<IActionResult> GetCurrentUser(
        [FromServices] ICurrentUserProvider userProvider,
        [FromServices] IReadRepository<User> userReader, CancellationToken token)
    {
        // TODO: if user provider will load it from DB then no need to load it from here 
        string email = userProvider?.CurrentUser?.Email;
        var user = (await userReader.GetAll(ByEmail(email), token, u => u.Roles)).FirstOrDefault();

        return Ok(user);
    }

}

