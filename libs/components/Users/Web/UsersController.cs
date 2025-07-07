
using Sencilla.Component.Security;

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
        [FromServices] IReadRepository<User> userReader,
        [FromServices] IReadRepository<Matrix> matrixReader, CancellationToken token)
    {
        // TODO: if user provider will load it from DB then no need to load it from here 
        string email = userProvider?.CurrentUser?.Email;
        var user = (await userReader.GetAll(ByEmail(email), token, u => u.Roles)).FirstOrDefault();

        //// check db dependencies 
        var roleIds = user?.Roles.Select(r => r.RoleId)?.ToArray() ?? [];
        if (roleIds.Any())
        {
            var matrix = (await matrixReader.GetAll(null, token))?.Where(m => roleIds.Contains(m.UserRoleId));
            var byRole = matrix?.ToLookup(m => m.UserRoleId);
            foreach (var role in user.Roles)
                role.Matrix = byRole[role.RoleId]?.ToList();
        }
        ////

        return Ok(user);
    }

}

