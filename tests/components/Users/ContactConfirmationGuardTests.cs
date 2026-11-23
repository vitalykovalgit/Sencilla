namespace Sencilla.Component.Users.Tests;

/// <summary>
/// <see cref="ContactConfirmationGuard"/>: a write as the current user can never raise a confirmation flag, keeps a
/// stored one only while its address is unchanged, and creates rows unconfirmed; a write under
/// <see cref="Access.Root"/> is left as written.
/// </summary>
public class ContactConfirmationGuardTests
{
    private readonly ContactConfirmationGuard _guard = new();

    private static readonly Guid Id = Guid.NewGuid();

    private static EntityUpdatingEvent<User> UpdateOf(User stored, User incoming)
        => new() { Entities = new[] { incoming }.AsQueryable(), DbEntities = new[] { stored }.AsQueryable() };

    private static EntityUpdatingEvent<UserPhone> UpdateOf(UserPhone stored, UserPhone incoming)
        => new() { Entities = new[] { incoming }.AsQueryable(), DbEntities = new[] { stored }.AsQueryable() };

    [Fact]
    public async Task UserUpdate_CannotRaiseEitherFlag()
    {
        var incoming = new User { Id = Id, Email = "a@example.com", EmailConf = true, Phone = 380501234567, PhoneConf = true };

        await _guard.HandleAsync(UpdateOf(new User { Id = Id, Email = "a@example.com", Phone = 380501234567 }, incoming), CancellationToken.None);

        Assert.False(incoming.EmailConf);
        Assert.False(incoming.PhoneConf);
    }

    [Fact]
    public async Task UserUpdate_KeepsConfirmedFlags_WhileTheAddressesAreUnchanged()
    {
        var stored = new User { Id = Id, Email = "a@example.com", EmailConf = true, Phone = 380501234567, PhoneConf = true };
        // The client neither raises nor clears a fact: flags it sends are ignored, an address differing only in case is the same.
        var incoming = new User { Id = Id, Email = " A@Example.com", Phone = 380501234567 };

        await _guard.HandleAsync(UpdateOf(stored, incoming), CancellationToken.None);

        Assert.True(incoming.EmailConf);
        Assert.True(incoming.PhoneConf);
    }

    [Fact]
    public async Task UserUpdate_DropsAFlag_WhenItsAddressChanges()
    {
        var stored = new User { Id = Id, Email = "a@example.com", EmailConf = true, Phone = 380501234567, PhoneConf = true };
        var incoming = new User { Id = Id, Email = "b@example.com", EmailConf = true, Phone = 380509999999, PhoneConf = true };

        await _guard.HandleAsync(UpdateOf(stored, incoming), CancellationToken.None);

        Assert.False(incoming.EmailConf);
        Assert.False(incoming.PhoneConf);
    }

    [Fact]
    public async Task UserCreate_AsTheCurrentUser_ArrivesUnconfirmed()
    {
        var created = new User { Id = Id, Email = "a@example.com", EmailConf = true, Phone = 380501234567, PhoneConf = true };

        await _guard.HandleAsync(new EntityCreatingEvent<User> { Entities = new[] { created }.AsQueryable() }, CancellationToken.None);

        Assert.False(created.EmailConf);
        Assert.False(created.PhoneConf);
    }

    [Fact]
    public async Task RootWrites_AreLeftAsWritten()
    {
        var updated = new User { Id = Id, Email = "b@example.com", EmailConf = true, Phone = 380509999999, PhoneConf = true };
        var created = new User { Id = Guid.NewGuid(), EmailConf = true, PhoneConf = true };
        var phone = new UserPhone { Id = Id, Phone = 380509999999, PhoneConf = true };

        using (Access.Root())
        {
            await _guard.HandleAsync(UpdateOf(new User { Id = Id }, updated), CancellationToken.None);
            await _guard.HandleAsync(new EntityCreatingEvent<User> { Entities = new[] { created }.AsQueryable() }, CancellationToken.None);
            await _guard.HandleAsync(UpdateOf(new UserPhone { Id = Id }, phone), CancellationToken.None);
        }

        Assert.True(updated.EmailConf && updated.PhoneConf);
        Assert.True(created.EmailConf && created.PhoneConf);
        Assert.True(phone.PhoneConf);
        Assert.Null(Access.Current);
    }

    [Fact]
    public async Task PhoneUpdate_KeepsAConfirmedNumber_DropsAChangedOne_NeverRaisesOne()
    {
        var confirmed = new UserPhone { Id = Id, Phone = 380501234567, PhoneConf = true };

        var same = new UserPhone { Id = Id, Phone = 380501234567 };
        await _guard.HandleAsync(UpdateOf(confirmed, same), CancellationToken.None);
        Assert.True(same.PhoneConf);

        var changed = new UserPhone { Id = Id, Phone = 380509999999, PhoneConf = true };
        await _guard.HandleAsync(UpdateOf(confirmed, changed), CancellationToken.None);
        Assert.False(changed.PhoneConf);

        var raised = new UserPhone { Id = Id, Phone = 380501234567, PhoneConf = true };
        await _guard.HandleAsync(UpdateOf(new UserPhone { Id = Id, Phone = 380501234567 }, raised), CancellationToken.None);
        Assert.False(raised.PhoneConf);
    }

    [Fact]
    public async Task TheEventRegistrator_WiresItForAllThreeWrites_AndEachEventReachesItsOwnOverload()
    {
        var services = new ServiceCollection();
        new EventRegistrator().Register(services, typeof(ContactConfirmationGuard));
        var events = new InMemoryMiddleware(services.BuildServiceProvider());

        var created = new User { EmailConf = true };
        var updated = new User { Id = Id, EmailConf = true };
        var phone = new UserPhone { Id = Id, Phone = 380501234567, PhoneConf = true };
        await events.ProcessAsync(new EntityCreatingEvent<User> { Entities = new[] { created }.AsQueryable() }, CancellationToken.None);
        await events.ProcessAsync(UpdateOf(new User { Id = Id }, updated), CancellationToken.None);
        await events.ProcessAsync(UpdateOf(new UserPhone { Id = Id, Phone = 380501234567 }, phone), CancellationToken.None);

        Assert.False(created.EmailConf);
        Assert.False(updated.EmailConf);
        Assert.False(phone.PhoneConf);
    }
}
