using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;

namespace Sencilla.Component.Users.Tests;

/// <summary><see cref="UsersController.GetCurrentUser"/> finds the caller's account by id — an account may have no email.</summary>
public class UsersControllerTests
{
    private readonly Mock<IReadRepository<User, Guid>> _users = new();
    private readonly UsersController _controller = new(Mock.Of<IServiceProvider>());

    private static ICurrentUserProvider CurrentUserIs(User user) => Mock.Of<ICurrentUserProvider>(p => p.CurrentUser == user);

    [Fact]
    public async Task GetCurrentUser_FindsAnAccountWithoutAnEmail_ById()
    {
        var stored = new User { Id = Guid.NewGuid() };
        _users.Setup(r => r.GetById(stored.Id, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(stored);

        var result = await _controller.GetCurrentUser(CurrentUserIs(new User { Id = stored.Id }), _users.Object, CancellationToken.None);

        Assert.Same(stored, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetCurrentUser_ForTheVisitor_IsNotFound_WithoutReading()
    {
        var result = await _controller.GetCurrentUser(CurrentUserIs(User.CreateAnonymous()), _users.Object, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        _users.VerifyNoOtherCalls();
    }
}
