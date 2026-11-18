using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.Authentication.Server.Tests;

public class CookieSessionTransportTests
{
    private static ClaimsPrincipal FamilyPrincipal(bool verified) => new(new ClaimsIdentity(new[]
    {
        new Claim(AuthClaims.Subject, "5b1a2f8e-0a1c-4a3a-9c2e-1234567890ab"),
        new Claim(AuthClaims.Email, "user@example.com"),
        new Claim(AuthClaims.EmailVerified, verified ? "true" : "false"),
        new Claim(AuthClaims.Roles, "User"),
        new Claim(AuthClaims.Roles, "Admin"),
        new Claim(AuthClaims.Name, "Ada"),
    }, "test"));

    [Fact]
    public async Task SignIn_SignsTheGivenPrincipalIntoTheCookieScheme_AsIs_AndReturnsTheUser()
    {
        var auth = new Mock<IAuthenticationService>();
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(auth.Object).BuildServiceProvider() };
        var transport = new CookieSessionTransport(Options.Create(new CookieSessionOptions()));
        var principal = FamilyPrincipal(verified: true);
        var claimCount = principal.Claims.Count();

        var result = await transport.SignIn(http, principal, new SessionLifetime(Persistent: true));

        var user = Assert.IsType<Ok<MeResponse>>(result).Value!;
        Assert.Equal("user@example.com", user.Email);
        Assert.Same(principal, http.User);
        Assert.Equal(claimCount, principal.Claims.Count());
        auth.Verify(a => a.SignInAsync(http, "Cookies", principal, It.Is<AuthenticationProperties>(p => p.IsPersistent)));
    }

    [Fact]
    public void SessionUser_ProjectsEitherVocabulary()
    {
        var family = SessionUser.Project(FamilyPrincipal(verified: true));
        Assert.Equal("5b1a2f8e-0a1c-4a3a-9c2e-1234567890ab", family.Id);
        Assert.Equal("user@example.com", family.Email);
        Assert.True(family.EmailVerified);
        Assert.Contains("Admin", family.Roles);

        // A bearer principal whose claims the handler mapped onto System.Security.Claims names.
        var mapped = SessionUser.Project(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5b1a2f8e-0a1c-4a3a-9c2e-1234567890ab"),
            new Claim(ClaimTypes.Email, "g@example.com"),
            new Claim(ClaimTypes.Role, "User"),
        }, "Bearer")));
        Assert.Equal("5b1a2f8e-0a1c-4a3a-9c2e-1234567890ab", mapped.Id);
        Assert.Equal("g@example.com", mapped.Email);
        Assert.False(mapped.EmailVerified);
        Assert.Contains("User", mapped.Roles);
    }
}
