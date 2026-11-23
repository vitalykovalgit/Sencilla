namespace Sencilla.Authentication.Server.Tests;

public class AccountLinkingPolicyTests
{
    private readonly AccountLinkingPolicy _policy = new();

    private AccountDecision Decide(AuthUser? byProvider, AuthUser? byEmail, ExternalIdentity incoming, AuthUser? connecting, bool isProviderOnConnecting = false)
        => _policy.Decide(byProvider, byEmail, incoming, connecting, isProviderOnConnecting);

    private static ExternalIdentity Identity(string? email = "user@example.com", bool emailVerified = true)
        => new("google", "google-sub-123", email, emailVerified);

    private static AuthUser User(AuthUserStatus status = AuthUserStatus.Active, string? email = "user@example.com", bool emailConfirmed = true)
        => new(Guid.NewGuid(), email, emailConfirmed, status);

    [Fact]
    public void KnownProvider_ActiveAccount_Links()
    {
        var existing = User();
        var decision = Decide(byProvider: existing, byEmail: null, Identity(), connecting: null);
        Assert.Equal(LinkAction.Link, decision.Action);
    }

    [Theory]
    [InlineData(AuthUserStatus.Disabled)]
    [InlineData(AuthUserStatus.Locked)]
    public void KnownProvider_UnusableAccount_Rejects(AuthUserStatus status)
    {
        var existing = User(status);
        var decision = Decide(byProvider: existing, byEmail: null, Identity(), connecting: null);
        Assert.Equal(LinkAction.Reject, decision.Action);
    }

    [Fact]
    public void KnownProvider_OwnedByDifferentSignedInUser_Rejects()
    {
        var owner = User();
        var decision = Decide(byProvider: owner, byEmail: null, Identity(), connecting: User());
        Assert.Equal(LinkAction.Reject, decision.Action);
        Assert.Equal("provider-linked-to-another-account", decision.Reason);
    }

    [Fact]
    public void KnownProvider_OwnedBySameSignedInUser_Links()
    {
        var owner = User();
        var decision = Decide(byProvider: owner, byEmail: null, Identity(), connecting: owner);
        Assert.Equal(LinkAction.Link, decision.Action);
    }

    [Fact]
    public void NewProvider_AuthenticatedUser_ConnectsToCurrentAccount()
    {
        var decision = Decide(byProvider: null, byEmail: null, Identity(), connecting: User());
        Assert.Equal(LinkAction.Connect, decision.Action);
        Assert.Equal("authenticated-link", decision.Reason);
    }

    [Fact]
    public void NewSubject_OfAProviderTheAuthenticatedUserAlreadyHas_IsRefused()
    {
        // One login per provider: a second Google is how a stolen session would add its own way in.
        var decision = Decide(byProvider: null, byEmail: null, Identity(), connecting: User(), isProviderOnConnecting: true);
        Assert.Equal(LinkAction.Reject, decision.Action);
        Assert.Equal("provider-already-connected", decision.Reason);
    }

    [Fact]
    public void NewProvider_AuthenticatedUserWithoutAnAddress_Connects()
    {
        var decision = Decide(byProvider: null, byEmail: null, Identity(), connecting: User(email: null, emailConfirmed: false));
        Assert.Equal(LinkAction.Connect, decision.Action);
    }

    [Fact]
    public void NewProvider_AuthenticatedUserWithAnUnconfirmedAddress_MustConfirmItFirst()
    {
        var decision = Decide(byProvider: null, byEmail: null, Identity(), connecting: User(emailConfirmed: false));
        Assert.Equal(LinkAction.Reject, decision.Action);
        Assert.Equal("confirm-email-first", decision.Reason);
    }

    [Fact]
    public void NewProvider_AuthenticatedUser_ConnectsEvenWhenTheProvidersAddressIsAnotherAccounts()
    {
        // The address stays the other account's: a connect only attaches the login.
        var decision = Decide(byProvider: null, byEmail: User(), Identity(), connecting: User());
        Assert.Equal(LinkAction.Connect, decision.Action);
    }

    [Fact]
    public void NewProvider_DisabledAuthenticatedUser_Rejects()
    {
        var decision = Decide(byProvider: null, byEmail: null, Identity(), connecting: User(AuthUserStatus.Disabled));
        Assert.Equal(LinkAction.Reject, decision.Action);
        Assert.Equal("account-not-active", decision.Reason);
    }

    [Fact]
    public void NewProvider_Anonymous_NoEmailCollision_Creates()
    {
        var decision = Decide(byProvider: null, byEmail: null, Identity(), connecting: null);
        Assert.Equal(LinkAction.Create, decision.Action);
    }

    [Fact]
    public void NewProvider_Anonymous_VerifiedEmailCollision_LinksByEmail()
    {
        // The provider proved the caller owns the address, so the login joins the account that owns it.
        var collidingAccount = User();
        var decision = Decide(byProvider: null, byEmail: collidingAccount, Identity(emailVerified: true), connecting: null);
        Assert.Equal(LinkAction.LinkByEmail, decision.Action);
    }

    [Fact]
    public void NewProvider_Anonymous_UnverifiedEmailCollision_Rejects()
    {
        // Whoever can put an address on a social profile must not get the account that owns it.
        var collidingAccount = User();
        var decision = Decide(byProvider: null, byEmail: collidingAccount, Identity(emailVerified: false), connecting: null);
        Assert.Equal(LinkAction.Reject, decision.Action);
        Assert.Equal("email-not-verified", decision.Reason);
    }

    [Fact]
    public void NewProvider_Anonymous_EmailCollisionWithDisabledAccount_Rejects()
    {
        var disabled = User(AuthUserStatus.Disabled);
        var decision = Decide(byProvider: null, byEmail: disabled, Identity(emailVerified: true), connecting: null);
        Assert.Equal(LinkAction.Reject, decision.Action);
        Assert.Equal("account-not-active", decision.Reason);
    }
}
