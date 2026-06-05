namespace Sencilla.Authentication.Jwt.Tests;

public class RefreshTokenServiceTests
{
    private static (RefreshTokenService service, InMemoryRefreshTokenStore store) Build(
        bool rotate = true, bool detectReuse = true, int lifetimeDays = 14)
    {
        var store = new InMemoryRefreshTokenStore();
        var service = new RefreshTokenService(store, Options.Create(new RefreshTokenOptions
        {
            Rotate = rotate,
            DetectReuse = detectReuse,
            LifetimeDays = lifetimeDays,
        }));
        return (service, store);
    }

    [Fact]
    public async Task Redeem_RotatesToNewToken()
    {
        var (service, _) = Build();
        var userId = Guid.NewGuid();

        var first = await service.Issue(userId);
        var result = await service.Redeem(first);

        Assert.Equal(RefreshOutcome.Rotated, result.Outcome);
        Assert.NotNull(result.Token);
        Assert.NotEqual(first, result.Token);
        Assert.Equal(userId, result.UserId);

        // The rotated token works for a subsequent refresh.
        var second = await service.Redeem(result.Token!);
        Assert.Equal(RefreshOutcome.Rotated, second.Outcome);
    }

    [Fact]
    public async Task Redeem_ReplayedOldToken_DetectsReuseAndRevokesFamily()
    {
        var (service, _) = Build();
        var userId = Guid.NewGuid();

        var first = await service.Issue(userId);
        var rotated = await service.Redeem(first);              // first -> second

        var replay = await service.Redeem(first);               // replay the consumed token
        Assert.Equal(RefreshOutcome.ReuseDetected, replay.Outcome);
        Assert.Equal(userId, replay.UserId);

        // The whole family is now revoked — the legitimately-rotated token is dead too.
        var afterBreach = await service.Redeem(rotated.Token!);
        Assert.Equal(RefreshOutcome.Invalid, afterBreach.Outcome);
    }

    [Fact]
    public async Task Redeem_UnknownToken_IsInvalid()
    {
        var (service, _) = Build();
        var result = await service.Redeem("not-a-real-token");
        Assert.Equal(RefreshOutcome.Invalid, result.Outcome);
    }

    [Fact]
    public async Task Redeem_ExpiredToken_IsInvalid()
    {
        var (service, store) = Build();
        var expired = new RefreshToken
        {
            Token = "expired-token",
            UserId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
        };
        await store.Add(expired);

        var result = await service.Redeem("expired-token");
        Assert.Equal(RefreshOutcome.Invalid, result.Outcome);
    }

    [Fact]
    public async Task Redeem_ReplayWithReuseDetectionDisabled_DoesNotRevokeFamily()
    {
        var (service, _) = Build(detectReuse: false);
        var userId = Guid.NewGuid();

        var first = await service.Issue(userId);
        var rotated = await service.Redeem(first);

        var replay = await service.Redeem(first);
        Assert.Equal(RefreshOutcome.ReuseDetected, replay.Outcome);

        // With detection off, the rotated token survives.
        var stillValid = await service.Redeem(rotated.Token!);
        Assert.Equal(RefreshOutcome.Rotated, stillValid.Outcome);
    }
}
