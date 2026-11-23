using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;

namespace Sencilla.Authentication.Integration.Tests;

/// <summary>
/// A host on a <c>TestServer</c> with a cookie session and one redirect sign-in over a mocked store — the provider's
/// real ASP.NET handler, only its back channel faked — and what a browser does with it.
/// </summary>
internal static class RedirectSignInHost
{
    public const string SignInFailedUrl = "https://app.test/login";

    /// <summary>Where a connect's challenge asks to come back to.</summary>
    public const string ProfileUrl = "/profile";

    /// <param name="useProvider">The <c>Use*</c> under test.</param>
    /// <param name="fakeProvider">Fakes the provider's back channel on its handler's options.</param>
    public static async Task<WebApplication> Start(
        Mock<IUserStore> store, Action<AuthenticationConfig> useProvider, Action<IServiceCollection> fakeProvider, IEventDispatcher? events = null)
    {
        // The session's principal is built for the account, roles included.
        store.Setup(s => s.GetRoles(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddSingleton(store.Object).AddSingleton(events ?? new Mock<IEventDispatcher>().Object);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        builder.Services.AddSencillaAuthentication(builder.Configuration, a => useProvider(a.UseApi(o => o.Disabled = true)));
        fakeProvider(builder.Services);

        var app = builder.Build();
        app.UseAuthentication();
        app.MapGet("/login/{scheme}", (string scheme) => Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [scheme]));
        // As a host's connect does: the account comes from its session — here, the test names it.
        app.MapGet("/connect/{scheme}/{accountId:guid}", (string scheme, Guid accountId) => Results.Challenge(
            new AuthenticationProperties { RedirectUri = ProfileUrl }.ConnectToAccountOf(SessionOf(accountId)), [scheme]));
        app.MapGet("/whoami", (HttpContext http) => http.User.FindFirst(AuthClaims.Subject)?.Value ?? "anonymous");
        await app.StartAsync();
        return app;
    }

    /// <summary>
    /// What a browser does: follow the host's challenge to the provider and come back to <paramref name="callbackPath"/>
    /// with the challenge's <c>state</c>, the provider's <paramref name="answer"/> and the cookies the challenge set —
    /// on the query, or posted as a form (<paramref name="posted"/>) as Apple does.
    /// </summary>
    public static async Task<(HttpResponseMessage Challenge, HttpResponseMessage Callback)> ComeBackFromProvider(
        HttpClient client, string challengePath, string callbackPath, string answer = "code=fake-code", IDictionary<string, string>? posted = null)
    {
        var challenge = await client.GetAsync(challengePath);
        var state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();

        var callback = posted is null
            ? new HttpRequestMessage(HttpMethod.Get, $"{callbackPath}?{answer}&state={Uri.EscapeDataString(state)}")
            : new HttpRequestMessage(HttpMethod.Post, callbackPath)
            {
                Content = new FormUrlEncodedContent(posted.Concat(QueryHelpers.ParseQuery(answer).Select(p => KeyValuePair.Create(p.Key, p.Value.ToString())))
                    .Append(KeyValuePair.Create("state", state))),
            };
        callback.Headers.Add("Cookie", CookiesOf(challenge));
        return (challenge, await client.SendAsync(callback));
    }

    /// <summary>The account whose session the callback's cookie carries, or <c>anonymous</c>.</summary>
    public static async Task<string> WhoIsSignedInBy(HttpClient client, HttpResponseMessage callback)
    {
        var whoami = new HttpRequestMessage(HttpMethod.Get, "/whoami");
        if (callback.Headers.TryGetValues("Set-Cookie", out _))
            whoami.Headers.Add("Cookie", CookiesOf(callback));
        return await (await client.SendAsync(whoami)).Content.ReadAsStringAsync();
    }

    public static bool IssuesASession(HttpResponseMessage callback)
        => callback.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.StartsWith(".AspNetCore.Cookies="));

    public static string CookiesOf(HttpResponseMessage response)
        => string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]));

    private static ClaimsPrincipal SessionOf(Guid accountId)
        => new(new ClaimsIdentity([new Claim(AuthClaims.Subject, accountId.ToString())], "test"));
}
