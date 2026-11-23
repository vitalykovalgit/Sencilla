namespace Sencilla.Authentication;

/// <summary>
/// A redirect sign-in the app registered: the provider key its accounts are linked under (<c>google</c>) and the
/// ASP.NET scheme a host challenges to start it (<c>Google</c>). Served, in registration order, by
/// <c>GET {BasePath}/providers</c>.
/// </summary>
public sealed record RedirectSignInProvider(string Provider, string Scheme);
