namespace Sencilla.Component.Users.Tests;

public class CurrentUserProviderTests
{
    [Fact]
    public void CurrentUser_WithoutAUserInSystemVariables_IsAnonymous_EvenForAPrincipalWithAnEmail()
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, "someone@test.com")], "test")) };
        var provider = new CurrentUserProvider(new HttpContextAccessor { HttpContext = http }, new Mock<ISystemVariable>().Object);

        var user = provider.CurrentUser;

        Assert.True(user.IsAnonymous());
        Assert.Equal((int)RoleType.Anonymous, user.Roles!.Single().RoleId);
    }
}
