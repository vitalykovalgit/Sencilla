namespace Sencilla.Authentication.Server.Tests;

public class AccountLinkingPolicyTests
{
    private readonly AccountLinkingPolicy _policy = new();

    private static ExternalIdentity Identity(string? email = "user@example.com", bool emailVerified = true)
        => new("google", "google-sub-123", email, emailVerified);

    private static AuthUser User(AuthUserStatus status = AuthUserStatus.Active, string? email = "user@example.com")
        => new(Guid.NewGuid(), email, EmailConfirmed: true, status);

    [Fact]
    public void KnownProvider_ActiveAccount_Links()
    {
        var existing = User();
        var decision = _policy.Decide(byProvider: existing, byEmail: null, Identity(), authenticatedUserId: null);
        Assert.Equal(LinkAction.Link, decision.Action);
    }

    [Theory]
    [InlineData(AuthUserStatus.Disabled)]
    [InlineData(AuthUserStatus.Locked)]
    public void KnownProvider_UnusableAccount_Rejects(AuthUserStatus status)
    {
        var existing = User(status);
        var decision = _policy.Decide(byProvider: existing, byEmail: null, Identity(), authenticatedUserId: null);
        Assert.Equal(LinkAction.Reject, decision.Action);
    }

    [Fact]
    public void KnownProvider_OwnedByDifferentSignedInUser_Rejects()
    {
        var owner = User();
        var decision = _policy.Decide(byProvider: owner, byEmail: null, Identity(), authenticatedUserId: Guid.NewGuid());
        Assert.Equal(LinkAction.Reject, decision.Action);
        Assert.Equal("provider-linked-to-another-account", decision.Reason);
    }

    [Fact]
    public void KnownProvider_OwnedBySameSignedInUser_Links()
    {
        var owner = User();
        var decision = _policy.Decide(byProvider: owner, byEmail: null, Identity(), authenticatedUserId: owner.Id);
        Assert.Equal(LinkAction.Link, decision.Action);
    }

    [Fact]
    public void NewProvider_AuthenticatedUser_LinksToCurrentAccount()
    {
        var decision = _policy.Decide(byProvider: null, byEmail: null, Identity(), authenticatedUserId: Guid.NewGuid());
        Assert.Equal(LinkAction.Link, decision.Action);
        Assert.Equal("authenticated-link", decision.Reason);
    }

    [Fact]
    public void NewProvider_Anonymous_NoEmailCollision_Creates()
    {
        var decision = _policy.Decide(byProvider: null, byEmail: null, Identity(), authenticatedUserId: null);
        Assert.Equal(LinkAction.Create, decision.Action);
    }

    [Fact]
    public void NewProvider_Anonymous_VerifiedEmailCollision_LinksByEmail()
    {
        // The provider proved the caller owns the address, so the login joins the account that owns it.
        var collidingAccount = User();
        var decision = _policy.Decide(byProvider: null, byEmail: collidingAccount, Identity(emailVerified: true), authenticatedUserId: null);
        Assert.Equal(LinkAction.LinkByEmail, decision.Action);
    }

    [Fact]
    public void NewProvider_Anonymous_UnverifiedEmailCollision_Rejects()
    {
        // Whoever can put an address on a social profile must not get the account that owns it.
        var collidingAccount = User();
        var decision = _policy.Decide(byProvider: null, byEmail: collidingAccount, Identity(emailVerified: false), authenticatedUserId: null);
        Assert.Equal(LinkAction.Reject, decision.Action);
        Assert.Equal("email-not-verified", decision.Reason);
    }

    [Fact]
    public void NewProvider_Anonymous_EmailCollisionWithDisabledAccount_Rejects()
    {
        var disabled = User(AuthUserStatus.Disabled);
        var decision = _policy.Decide(byProvider: null, byEmail: disabled, Identity(emailVerified: true), authenticatedUserId: null);
        Assert.Equal(LinkAction.Reject, decision.Action);
        Assert.Equal("account-not-active", decision.Reason);
    }
}
