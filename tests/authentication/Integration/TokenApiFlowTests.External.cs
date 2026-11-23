namespace Sencilla.Authentication.Integration.Tests;

/// <summary>External identities over the real EF bridge: exchange, names, linking, the provider's phone and address, connect.</summary>
public sealed partial class TokenApiFlowTests
{
    [Fact]
    public async Task ExternalExchange_KeepsNameAndPicture()
    {
        await _external.Exchange("google", "fake-id-token");
        var row = _db.Users.AsNoTracking().Single(u => u.Email == "ext@example.com");
        Assert.Equal("Ext", row.FirstName);
        Assert.Equal("User", row.LastName);
    }

    [Fact]
    public async Task ExternalSignIn_KeepsACompoundGivenNameWhole_AndTheFamilyNameAsTheLastName()
    {
        await _external.ResolveAccount(new ExternalIdentity("google", "google-sub-anna", "anna@example.com", EmailVerified: true,
            Name: "Anna Maria Koval", GivenName: "Anna Maria", FamilyName: "Koval"));

        var row = _db.Users.AsNoTracking().Single(u => u.Email == "anna@example.com");
        Assert.Equal("Anna Maria", row.FirstName);
        Assert.Equal("Koval", row.LastName);
    }

    [Fact]
    public async Task ExternalExchange_CreatesUser_ThenLinksSameAccount()
    {
        var first = await _external.Exchange("google", "fake-id-token");

        var linked = await _store.FindByProviderKey("google", "google-sub-xyz");
        Assert.NotNull(linked);
        Assert.Equal(linked!.Id, first.Id);
        Assert.Equal("ext@example.com", linked.Email);

        // Signing in again with the same provider identity resolves to the same account.
        var second = await _external.Exchange("google", "fake-id-token");
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task LinkProvider_AdoptsALegacyRowInsteadOfDuplicating()
    {
        // What the first-login middleware wrote for years: scheme name, no subject.
        var user = _db.Users.Add(new User { Id = Guid.NewGuid(), Email = "legacy@example.com", EmailConf = true, Status = 2 }).Entity;
        _db.UserAuths.Add(new UserAuth { Id = Guid.NewGuid(), UserId = user.Id, Auth = "Google", Email = user.Email });
        await _db.SaveChangesAsync();
        NewRequest();

        await _store.LinkProvider(user.Id, new ExternalIdentity("google", "sub-legacy", "legacy@example.com", true));

        var rows = _db.UserAuths.Where(a => a.UserId == user.Id).ToList();
        Assert.Single(rows);
        Assert.Equal("sub-legacy", rows[0].ProviderKey);
        Assert.NotNull(await _store.FindByProviderKey("google", "sub-legacy"));
    }

    [Fact]
    public async Task ExternalSignIn_WritesTheProvidersPhone_ConfirmedOnlyOnTheFirstAccountThatHasIt_AndNeverOverAnExistingOne()
    {
        var first = await _external.ResolveAccount(new ExternalIdentity("telegram", "tg-1", null, false, Phone: 380501234567, PhoneVerified: true));
        var second = await _external.ResolveAccount(new ExternalIdentity("telegram", "tg-2", null, false, Phone: 380501234567, PhoneVerified: true));
        // The first account signs in again with another number: its phone is the account's, not the provider's.
        await _external.ResolveAccount(new ExternalIdentity("telegram", "tg-1", null, false, Phone: 380509999999, PhoneVerified: true));

        var firstRow = _db.Users.AsNoTracking().Single(u => u.Id == first.Id);
        var secondRow = _db.Users.AsNoTracking().Single(u => u.Id == second.Id);
        Assert.Equal((380501234567, true), (firstRow.Phone, firstRow.PhoneConf));
        Assert.Equal((380501234567, false), (secondRow.Phone, secondRow.PhoneConf));
    }

    [Fact]
    public async Task ExternalSignIn_WithAnUnverifiedAddress_StartsTheAccountWithoutIt_AndMailsOurLinkToItAsPending()
    {
        // Facebook: an address, no verified flag.
        var account = await _external.ResolveAccount(new ExternalIdentity("facebook", "fb-1", "Ada@Example.com", EmailVerified: false, "Ada Lovelace"));

        Assert.Null(_db.Users.AsNoTracking().Single(u => u.Id == account.Id).Email);
        Assert.Equal("ada@example.com", await _recovery.FindPendingEmail(account.Id));
        Assert.Equal((account.Id, "ada@example.com"), _events.Of<EmailVerificationRequested>().Select(e => (e.UserId, e.Email)).Single());

        NewRequest();
        var confirmed = await _recovery.ConfirmEmail(_events.Of<EmailVerificationRequested>().Single().Token);
        Assert.Equal(("ada@example.com", true), (confirmed.Email, confirmed.EmailConfirmed));
    }

    [Fact]
    public async Task LinkedProviders_AreEachListedOnce_LowerCased_WithoutThePassword()
    {
        await _credentials.Register(new RegisterRequest { Email = "multi@example.com", Password = "Sup3rSecret!" });
        NewRequest();
        var user = (await _store.FindByEmail("multi@example.com"))!;
        _db.UserAuths.Add(new UserAuth { Id = Guid.NewGuid(), UserId = user.Id, Auth = "Google", Email = user.Email });   // a legacy row
        _db.UserAuths.Add(new UserAuth { Id = Guid.NewGuid(), UserId = user.Id, Auth = "google", ProviderKey = "g-1" });
        _db.UserAuths.Add(new UserAuth { Id = Guid.NewGuid(), UserId = user.Id, Auth = "telegram", ProviderKey = "tg-1" });
        await _db.SaveChangesAsync();
        NewRequest();

        Assert.Equal(["google", "telegram"], (await _store.GetLinkedProviders(user.Id)).Order());
        Assert.True(await _store.HasPasswordCredential(user.Id));
    }

    [Fact]
    public async Task ConfirmedPhone_OnAnotherAccount_IsFoundOnlyForTheConfirmedOne_AndNeverOnADeletedAccount()
    {
        var typed = _db.Users.Add(new User { Id = Guid.NewGuid(), Phone = 380501234567, Status = 2 }).Entity;
        await _db.SaveChangesAsync();
        NewRequest();
        var telegram = await _external.ResolveAccount(new ExternalIdentity("telegram", "tg-hint", null, false, Phone: 380501234567, PhoneVerified: true));
        var other = await _external.ResolveAccount(new ExternalIdentity("telegram", "tg-none", null, false, Phone: 380507654321, PhoneVerified: true));

        Assert.True(await _store.IsConfirmedPhoneOnAnotherAccount(telegram.Id));
        Assert.False(await _store.IsConfirmedPhoneOnAnotherAccount(typed.Id));       // its phone is only typed
        Assert.False(await _store.IsConfirmedPhoneOnAnotherAccount(other.Id));       // nobody else has it

        var deleted = _db.Users.Single(u => u.Id == typed.Id);
        deleted.DeletedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        NewRequest();
        Assert.False(await _store.IsConfirmedPhoneOnAnotherAccount(telegram.Id));
    }

    [Fact]
    public async Task ConnectingTelegram_ConfirmsTheAccountsOwnNumber_WritesOneWhereThereIsNone_AndNeverTouchesAnother()
    {
        var same = AddAccountWithPhone(380501111111);
        var none = AddAccountWithPhone(0);
        var other = AddAccountWithPhone(380502222222);
        await _db.SaveChangesAsync();
        NewRequest();

        await _external.ResolveAccount(TelegramWithPhone("tg-same", 380501111111), connectTo: same);
        await _external.ResolveAccount(TelegramWithPhone("tg-none", 380503333333), connectTo: none);
        await _external.ResolveAccount(TelegramWithPhone("tg-other", 380504444444), connectTo: other);

        Assert.Equal((380501111111, true), PhoneOf(same));
        Assert.Equal((380503333333, true), PhoneOf(none));
        Assert.Equal((380502222222, false), PhoneOf(other));
    }

    [Fact]
    public async Task ConnectingTelegram_WithANumberConfirmedOnAnotherAccount_DoesNotConfirmItHere()
    {
        await _external.ResolveAccount(TelegramWithPhone("tg-owner", 380505555555));   // the first account confirms it
        var typed = AddAccountWithPhone(380505555555);
        var none = AddAccountWithPhone(0);
        await _db.SaveChangesAsync();
        NewRequest();

        await _external.ResolveAccount(TelegramWithPhone("tg-second", 380505555555), connectTo: typed);
        await _external.ResolveAccount(TelegramWithPhone("tg-third", 380505555555), connectTo: none);

        Assert.Equal((380505555555, false), PhoneOf(typed));
        // As at account creation: the number is written, unconfirmed.
        Assert.Equal((380505555555, false), PhoneOf(none));
    }

    [Fact]
    public async Task Connect_AllowsOneLoginPerProvider_ButTheSameSubjectAgain_AndAnotherProvider()
    {
        var account = await _external.ResolveAccount(new ExternalIdentity("google", "g-mine", "mine@example.com", EmailVerified: true));
        NewRequest();

        var secondGoogle = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _external.ResolveAccount(new ExternalIdentity("google", "g-someone-else", "else@example.com", EmailVerified: true), connectTo: account.Id));
        var sameGoogle = await _external.ResolveAccount(new ExternalIdentity("google", "g-mine", "mine@example.com", EmailVerified: true), connectTo: account.Id);
        var telegram = await _external.ResolveAccount(new ExternalIdentity("telegram", "tg-mine", null, false), connectTo: account.Id);

        Assert.Equal("provider-already-connected", secondGoogle.Message);
        Assert.Equal((account.Id, account.Id), (sameGoogle.Id, telegram.Id));
        Assert.Equal(["google", "telegram"], (await _store.GetLinkedProviders(account.Id)).Order());
        Assert.Null(await _store.FindByProviderKey("google", "g-someone-else"));
    }

    private Guid AddAccountWithPhone(long phone)
        => _db.Users.Add(new User { Id = Guid.NewGuid(), Phone = phone, Status = 2 }).Entity.Id;

    private (long Phone, bool IsConfirmed) PhoneOf(Guid accountId)
    {
        var user = _db.Users.AsNoTracking().Single(u => u.Id == accountId);
        return (user.Phone, user.PhoneConf);
    }

    private static ExternalIdentity TelegramWithPhone(string subject, long phone)
        => new("telegram", subject, Email: null, EmailVerified: false, Phone: phone, PhoneVerified: true);
}
