namespace Sencilla.Authentication;

/// <summary>
/// Instagram configuration, bound from <c>"Authentication:Instagram"</c>: the app's «Business Login for Instagram»
/// client — <c>ClientId</c> (the Instagram App ID) and <c>ClientSecret</c> (the Instagram App Secret). A redirect
/// sign-in is Instagram's only shape, so <see cref="RedirectSignInOptions.RedirectSignIn"/> starts on.
/// </summary>
public sealed class InstagramProviderOptions : RedirectSignInOptions
{
    public InstagramProviderOptions() => RedirectSignIn = true;
}
