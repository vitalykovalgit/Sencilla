namespace Sencilla.Authentication.Integration.Tests;

/// <summary>The pending address of an account that has none: issued, replaced, taken meanwhile, expired.</summary>
public sealed partial class TokenApiFlowTests
{
    [Fact]
    public async Task PendingEmail_ReachesTheAccountOnlyWhenTheLinkIsFollowed()
    {
        var account = await AccountWithoutAnEmail("tg-pending");
        Assert.Null(await _recovery.FindPendingEmail(account.Id));

        await _recovery.RequestEmailVerification(account.Id, " New@Example.com ");
        var typed = _events.Of<EmailVerificationRequested>().Last();
        Assert.Equal("new@example.com", typed.Email);
        Assert.Equal("new@example.com", await _recovery.FindPendingEmail(account.Id));
        Assert.Null((await _store.FindById(account.Id))!.Email);

        // «Change»: the new address replaces the pending one, and the first link is dead.
        await _recovery.RequestEmailVerification(account.Id, "other@example.com");
        Assert.Equal("other@example.com", await _recovery.FindPendingEmail(account.Id));
        Assert.Equal("token-already-used", (await Assert.ThrowsAsync<BadRequestException>(() => _recovery.ConfirmEmail(typed.Token))).Message);

        // «Send again» with no address goes to the pending one.
        await _recovery.RequestEmailVerification(account.Id);
        var resent = _events.Of<EmailVerificationRequested>().Last();
        Assert.Equal("other@example.com", resent.Email);
        Assert.Equal(3, _events.Of<EmailVerificationRequested>().Count());

        NewRequest();
        var confirmed = await _recovery.ConfirmEmail(resent.Token);
        Assert.Equal(("other@example.com", true), (confirmed.Email, confirmed.EmailConfirmed));
        Assert.Null(await _recovery.FindPendingEmail(account.Id));
    }

    [Fact]
    public async Task PendingEmail_TakenByAnotherAccountMeanwhile_IsRefusedAtTheClick()
    {
        var account = await AccountWithoutAnEmail("tg-late");
        await _recovery.RequestEmailVerification(account.Id, "contested@example.com");
        var link = _events.Of<EmailVerificationRequested>().Last();
        await _credentials.Register(new RegisterRequest { Email = "contested@example.com", Password = "Sup3rSecret!" });
        NewRequest();

        Assert.Equal("email-taken", (await Assert.ThrowsAsync<BadRequestException>(() => _recovery.ConfirmEmail(link.Token))).Message);
        Assert.Null((await _store.FindById(account.Id))!.Email);
    }

    [Fact]
    public async Task PendingEmail_IsRefused_ForAMalformedAddress_AnotherAccountsAddress_AndAnAccountThatHasOne()
    {
        var account = await AccountWithoutAnEmail("tg-refused");
        var registered = await _credentials.Register(new RegisterRequest { Email = "owned@example.com", Password = "Sup3rSecret!" });
        var mailed = _events.Of<EmailVerificationRequested>().Count();

        Assert.Equal("email-invalid", (await Assert.ThrowsAsync<BadRequestException>(() => _recovery.RequestEmailVerification(account.Id, "Support <owned@example.com>"))).Message);
        Assert.Equal("email-taken", (await Assert.ThrowsAsync<BadRequestException>(() => _recovery.RequestEmailVerification(account.Id, "Owned@Example.com"))).Message);
        Assert.Equal("email-already-set", (await Assert.ThrowsAsync<BadRequestException>(() => _recovery.RequestEmailVerification(registered.Id, "else@example.com"))).Message);
        Assert.Equal(mailed, _events.Of<EmailVerificationRequested>().Count());
        Assert.Null(await _recovery.FindPendingEmail(account.Id));

        // The account's own address typed again is no pending address: the link goes to it, as with none typed.
        await _recovery.RequestEmailVerification(registered.Id, "OWNED@example.com");
        Assert.Equal("owned@example.com", _events.Of<EmailVerificationRequested>().Last().Email);
        Assert.Null(await _recovery.FindPendingEmail(registered.Id));
    }

    [Fact]
    public async Task PendingEmail_StaysPending_AfterItsLinkExpires()
    {
        _accounts.VerificationTokenLifetime = TimeSpan.FromMinutes(-1);       // already expired when issued
        var account = await AccountWithoutAnEmail("tg-expired");

        await _recovery.RequestEmailVerification(account.Id, "slow@example.com");

        Assert.Equal("token-expired", (await Assert.ThrowsAsync<BadRequestException>(() => _recovery.ConfirmEmail(_events.Of<EmailVerificationRequested>().Last().Token))).Message);
        Assert.Equal("slow@example.com", await _recovery.FindPendingEmail(account.Id));
    }

    /// <summary>A social sign-in that gave no contact, as Instagram or a Telegram sign-in without the phone does.</summary>
    private Task<AuthUser> AccountWithoutAnEmail(string subject)
        => _external.ResolveAccount(new ExternalIdentity("telegram", subject, Email: null, EmailVerified: false));
}
