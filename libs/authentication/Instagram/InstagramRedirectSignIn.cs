namespace Sencilla.Authentication;

/// <summary>
/// «Business Login for Instagram» on ASP.NET's generic OAuth handler: endpoints and scope from Meta's Business Login
/// page, the profile from the Graph host's <c>/me</c>, and how it reads as an <see cref="ExternalIdentity"/> for the
/// shared binding (<see cref="AuthenticationConfig.AddRedirectSignIn"/>). Instagram gives no address and no phone.
/// </summary>
internal static class InstagramRedirectSignIn
{
    public const string ProfileFields = "user_id,username,profile_picture_url";

    public static void Configure(OAuthOptions options, InstagramProviderOptions instagram)
    {
        options.ClientId = instagram.ClientId!;
        options.ClientSecret = instagram.ClientSecret!;
        options.CallbackPath = "/signin-instagram";
        options.AuthorizationEndpoint = "https://www.instagram.com/oauth/authorize";
        options.TokenEndpoint = "https://api.instagram.com/oauth/access_token";
        // UNVERIFIED: the Get Started page's sample is versioned (v27.0); unversioned is the app's default version.
        options.UserInformationEndpoint = "https://graph.instagram.com/me";
        options.Scope.Add("instagram_business_basic");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "user_id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
        options.ClaimActions.MapJsonKey(AuthClaims.Picture, "profile_picture_url");
    }

    public static ExternalIdentity ReadIdentity(ClaimsPrincipal instagram) => new(
        Provider: "instagram",
        Subject: instagram.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Instagram returned no user id, so the sign-in cannot be bound to an account."),
        Email: null,
        EmailVerified: false,
        // The username is the only name Instagram gives; it is never split into a first and a last name.
        Name: instagram.FindFirstValue(ClaimTypes.Name),
        Picture: instagram.FindFirstValue(AuthClaims.Picture));

    /// <summary>
    /// The entry an Instagram answer carries: Meta's samples wrap both the code exchange and <c>/me</c> in
    /// <c>{ "data": [ { … } ] }</c>; a flat object is taken as it is.
    /// </summary>
    public static JsonElement FirstEntryOf(JsonElement answer)
        => answer.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0
            ? data[0]
            : answer;
}
