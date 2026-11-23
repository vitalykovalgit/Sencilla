namespace Sencilla.Authentication;

/// <summary>
/// What Sign in with Apple's redirect sign-in adds to the shared binding
/// (<see cref="AuthenticationConfig.AddRedirectSignIn"/>), on <c>AspNet.Security.OAuth.Apple</c>: the client secret
/// signed with the configured key, the name Apple posts once, and how the principal — the <c>id_token</c>'s claims —
/// reads as an <see cref="ExternalIdentity"/>. <c>email_verified</c> and <c>is_private_email</c> read as the
/// <see cref="AppleTokenVerifier"/> reads them: a relay address is a verified address.
/// </summary>
internal static class AppleRedirectSignIn
{
    public static void Configure(AppleAuthenticationOptions options, AppleProviderOptions apple)
    {
        options.ClientId = apple.ClientId!;
        options.TeamId = apple.TeamId!;
        options.KeyId = apple.KeyId!;
        options.GenerateClientSecret = true;
        var privateKey = PrivateKeyAsPem(apple.PrivateKey!);
        options.PrivateKey = (_, _) => Task.FromResult(privateKey.AsMemory());
        options.Events.OnCreatingTicket = context =>
        {
            AddPostedName(context);
            return Task.CompletedTask;
        };
    }

    public static ExternalIdentity ReadIdentity(ClaimsPrincipal apple) => new(
        Provider: "apple",
        Subject: apple.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Apple returned no subject, so the sign-in cannot be bound to an account."),
        Email: apple.FindFirstValue("email"),
        EmailVerified: IsTrue(apple, "email_verified"),
        RawClaims: IsTrue(apple, "is_private_email") ? new Dictionary<string, string> { ["is_private_email"] = "true" } : null,
        GivenName: apple.FindFirstValue(ClaimTypes.GivenName),
        FamilyName: apple.FindFirstValue(ClaimTypes.Surname));

    /// <summary>
    /// The name is never in the <c>id_token</c>: Apple posts it once, on the first sign-in, as the form field
    /// <c>user</c> = <c>{ "name": { "firstName", "lastName" } }</c> — typed by the user themselves, so it is only ever
    /// their own account's name.
    /// </summary>
    private static void AddPostedName(OAuthCreatingTicketContext context)
    {
        if (!context.Request.HasFormContentType || context.Request.Form["user"].ToString() is not { Length: > 0 } posted)
            return;
        try
        {
            using var user = JsonDocument.Parse(posted);
            if (!user.RootElement.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.Object)
                return;
            if (name.TryGetProperty("firstName", out var first) && first.GetString() is { Length: > 0 } given)
                context.Identity!.AddClaim(new Claim(ClaimTypes.GivenName, given));
            if (name.TryGetProperty("lastName", out var last) && last.GetString() is { Length: > 0 } family)
                context.Identity!.AddClaim(new Claim(ClaimTypes.Surname, family));
        }
        catch (JsonException)
        {
            // A malformed name costs only the name.
        }
    }

    /// <summary>
    /// The .p8 key as PEM, from any shape a one-line env value gives it: the PEM itself, the PEM with literal <c>\n</c>
    /// for its line breaks, or the bare base64 body without the BEGIN / END lines.
    /// </summary>
    private static string PrivateKeyAsPem(string configured)
    {
        var key = configured.Replace("\\n", "\n").Trim();
        return key.StartsWith("-----BEGIN", StringComparison.Ordinal) ? key : $"-----BEGIN PRIVATE KEY-----\n{key}\n-----END PRIVATE KEY-----";
    }

    // Apple sends these as JSON strings ("true"), sometimes as booleans.
    private static bool IsTrue(ClaimsPrincipal apple, string claim)
        => bool.TryParse(apple.FindFirstValue(claim), out var value) && value;
}
