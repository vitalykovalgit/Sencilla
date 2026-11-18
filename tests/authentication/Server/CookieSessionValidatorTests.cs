using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sencilla.Component.Users;

namespace Sencilla.Authentication.Server.Tests;

/// <summary>
/// The end of a session by a credential change after it started, and the upgrade of tickets issued
/// before the account id was in the session (an email, no id).
/// </summary>
public class CookieSessionValidatorTests
{
    // Half a second past the whole one, as a real DATETIME2 stamp is: ASP.NET keeps IssuedUtc to 10:00:00.
    private static readonly DateTime CredentialsChangedAt = new(2026, 11, 17, 10, 0, 0, 500, DateTimeKind.Utc);
    private static readonly DateTimeOffset IssuedInTheSameSecond = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserStore> _store = new();
    private readonly Mock<IAuthenticationService> _auth = new();

    [Theory]
    [InlineData(1, true)]
    [InlineData(-1, false)]
    public async Task SessionStartedAroundACredentialChange_IsKeptOnlyWhenItStartedAfterIt(int startedAfterChangeMs, bool kept)
    {
        var properties = new AuthenticationProperties { IssuedUtc = IssuedInTheSameSecond };
        properties.Items["sencilla.started"] = new DateTimeOffset(CredentialsChangedAt.AddMilliseconds(startedAfterChangeMs)).ToString("o");

        Assert.Equal(kept, await IsSessionKept(properties));
    }

    [Fact]
    public async Task SessionStartedBeforeACredentialChange_IsEnded_EvenWhenARenewalMovedItsIssuedUtcPastTheChange()
    {
        var properties = new AuthenticationProperties { IssuedUtc = new DateTimeOffset(CredentialsChangedAt).AddMinutes(1) };
        properties.Items["sencilla.started"] = new DateTimeOffset(CredentialsChangedAt).AddMinutes(-1).ToString("o");

        Assert.False(await IsSessionKept(properties));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(-1, false)]
    public async Task TicketWithoutAStartStamp_IsJudgedByItsIssuedUtc(int issuedAfterChangeSeconds, bool kept)
    {
        var properties = new AuthenticationProperties { IssuedUtc = new DateTimeOffset(CredentialsChangedAt).AddSeconds(issuedAfterChangeSeconds) };

        Assert.Equal(kept, await IsSessionKept(properties));
    }

    [Fact]
    public async Task EverySignIn_StampsTheSessionStartAsItsValidation_AndTheHostsOwnSigningInStillRuns()
    {
        var hostSigningInRan = false;
        var services = new ServiceCollection().AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddAuthentication().AddCookie(o => o.Events.OnSigningIn = _ => { hostSigningInRan = true; return Task.CompletedTask; });
        services.AddSencillaAuthentication(new ConfigurationBuilder().Build(), a => a.UseCookieSession());
        var cookie = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        var properties = new AuthenticationProperties();
        var before = DateTimeOffset.UtcNow;

        await cookie.Events.SigningIn(new CookieSigningInContext(new DefaultHttpContext(), scheme, cookie, new ClaimsPrincipal(), properties, new CookieOptions()));

        Assert.True(hostSigningInRan);
        Assert.InRange(DateTimeOffset.Parse(properties.Items["sencilla.started"]!, null, DateTimeStyles.RoundtripKind), before, DateTimeOffset.UtcNow);
        Assert.Equal(properties.Items["sencilla.started"], properties.Items["sencilla.validated"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SessionFreshFromSignIn_IsReadFromTheStoreAndRenewed_OnlyOnceTheValidationIntervalHasPassed(bool intervalPassed)
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        _store.Setup(s => s.FindById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var context = CreateContextFreshFromSignIn(user);

        await CookieSessionValidator.Validate(context, new CookieSessionOptions { ValidationInterval = intervalPassed ? TimeSpan.Zero : TimeSpan.FromMinutes(5) });

        Assert.NotNull(context.Principal);
        Assert.Equal(intervalPassed, context.ShouldRenew);
        _store.Verify(s => s.FindById(user.Id, It.IsAny<CancellationToken>()), intervalPassed ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task SessionFreshFromSignIn_IsEndedByACredentialChangeAfterIt_OnceTheValidationIntervalHasPassed()
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, AuthUserStatus.Active, CredentialsChangedAt: DateTime.UtcNow.AddSeconds(1));
        _store.Setup(s => s.FindById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var context = CreateContextFreshFromSignIn(user);

        await CookieSessionValidator.Validate(context, new CookieSessionOptions { ValidationInterval = TimeSpan.Zero });

        Assert.Null(context.Principal);
    }

    private CookieValidatePrincipalContext CreateContextFreshFromSignIn(AuthUser user)
    {
        var properties = new AuthenticationProperties();
        CookieSessionValidator.RecordSessionStartAsValidated(properties);
        return CreateContext(properties, new Claim(AuthClaims.Subject, user.Id.ToString()));
    }

    private async Task<bool> IsSessionKept(AuthenticationProperties properties)
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, AuthUserStatus.Active, CredentialsChangedAt: CredentialsChangedAt);
        _store.Setup(s => s.FindById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var context = CreateContext(properties, new Claim(AuthClaims.Subject, user.Id.ToString()));

        await CookieSessionValidator.Validate(context, new CookieSessionOptions());

        return context.Principal is not null;
    }

    private CookieValidatePrincipalContext CreateContext(params Claim[] claims) => CreateContext(new AuthenticationProperties(), claims);

    private CookieValidatePrincipalContext CreateContext(AuthenticationProperties properties, params Claim[] claims)
    {
        _store.Setup(s => s.GetRoles(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(["User"]);
        var services = new ServiceCollection()
            .AddSingleton(_store.Object)
            .AddSingleton(_auth.Object)
            .AddSingleton<IClaimsPrincipalFactory>(new ClaimsPrincipalFactory(_store.Object, []))
            .BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")), properties, scheme.Name);
        return new CookieValidatePrincipalContext(http, scheme, new CookieAuthenticationOptions(), ticket);
    }

    [Fact]
    public async Task EmailOnlyTicket_OfAnExistingUser_IsReplacedByTheAccountsPrincipal_AndRenewed()
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        _store.Setup(s => s.FindByEmail("ada@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var context = CreateContext(new Claim(ClaimTypes.NameIdentifier, "1234567890"), new Claim(ClaimTypes.Email, "ada@example.com"));

        await CookieSessionValidator.Validate(context, new CookieSessionOptions());

        Assert.Equal(user.Id, context.Principal!.GetUserId());
        Assert.Equal("ada@example.com", context.Principal!.FindFirst(AuthClaims.Email)!.Value);
        Assert.True(context.ShouldRenew);
        Assert.True(context.Properties.Items.ContainsKey("sencilla.validated"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(AuthUserStatus.Disabled)]
    [InlineData(AuthUserStatus.Locked)]
    public async Task EmailOnlyTicket_WithoutAUsableAccount_IsRejected(AuthUserStatus? status)
    {
        var user = status is { } s ? new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, s) : null;
        _store.Setup(s => s.FindByEmail("ada@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var context = CreateContext(new Claim(ClaimTypes.Email, "ada@example.com"));

        await CookieSessionValidator.Validate(context, new CookieSessionOptions());

        Assert.Null(context.Principal);
        _auth.Verify(a => a.SignOutAsync(context.HttpContext, "Cookies", It.IsAny<AuthenticationProperties?>()), Times.Once);
    }

    [Fact]
    public async Task TicketWithAnId_IsNotLookedUpByEmail()
    {
        var user = new AuthUser(Guid.NewGuid(), "ada@example.com", EmailConfirmed: true, AuthUserStatus.Active);
        _store.Setup(s => s.FindById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var context = CreateContext(new Claim(AuthClaims.Subject, user.Id.ToString()), new Claim(ClaimTypes.Email, "ada@example.com"));
        var principal = context.Principal;

        await CookieSessionValidator.Validate(context, new CookieSessionOptions());

        Assert.Same(principal, context.Principal);
        _store.Verify(s => s.FindByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
