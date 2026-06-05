namespace Sencilla.Authentication.Server.Tests;

public class ClaimsPrincipalFactoryTests
{
    [Fact]
    public async Task Create_WritesSubjectEmailAndRoles()
    {
        var user = new AuthUser(Guid.NewGuid(), "user@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        var store = new Mock<IUserStore>();
        store.Setup(s => s.GetRoles(user.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new[] { "User", "Admin" });

        var principal = await new ClaimsPrincipalFactory(store.Object, Array.Empty<IClaimsEnricher>()).Create(user);

        Assert.Equal(user.Id.ToString(), principal.FindFirst(AuthClaims.Subject)!.Value);
        Assert.Equal("user@example.com", principal.FindFirst(AuthClaims.Email)!.Value);
        Assert.Equal("true", principal.FindFirst(AuthClaims.EmailVerified)!.Value);

        var roles = principal.FindAll(AuthClaims.Roles).Select(c => c.Value).ToArray();
        Assert.Contains("User", roles);
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public async Task Create_AddsScopesAndRunsEnrichers()
    {
        var user = new AuthUser(Guid.NewGuid(), Email: null, EmailConfirmed: false, AuthUserStatus.Active);
        var store = new Mock<IUserStore>();
        store.Setup(s => s.GetRoles(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Array.Empty<string>());

        var factory = new ClaimsPrincipalFactory(store.Object, new IClaimsEnricher[] { new TestEnricher() });
        var principal = await factory.Create(user, scopes: new[] { "albums.read" });

        Assert.Equal("albums.read", principal.FindFirst("scope")!.Value);
        Assert.Equal("yes", principal.FindFirst("enriched")!.Value);
        Assert.Null(principal.FindFirst(AuthClaims.Email)); // no email claim when the user has none
    }

    private sealed class TestEnricher : IClaimsEnricher
    {
        public Task Enrich(ICollection<Claim> claims, AuthUser user, CancellationToken token = default)
        {
            claims.Add(new Claim("enriched", "yes"));
            return Task.CompletedTask;
        }
    }
}
