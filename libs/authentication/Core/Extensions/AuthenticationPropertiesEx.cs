namespace Microsoft.AspNetCore.Authentication;

/// <summary>The connect intent of a redirect sign-in, carried in the challenge's protected state.</summary>
public static class AuthenticationPropertiesEx
{
    private const string AccountToConnectKey = ".sencilla.connect";

    /// <summary>
    /// Make this challenge a connect: the provider the user signs in with is attached to the account of
    /// <paramref name="session"/> — the host's signed-in principal, never a value taken from the request — instead of
    /// signing in as whichever account that provider identity resolves to. The intent rides the challenge's protected
    /// state, so the callback reads it back without the session cookie (Apple posts its answer cross-site). A
    /// principal without an account id leaves the challenge an ordinary sign-in.
    /// </summary>
    public static AuthenticationProperties ConnectToAccountOf(this AuthenticationProperties properties, ClaimsPrincipal session)
    {
        if (session.GetUserId() is { } userId)
            properties.Items[AccountToConnectKey] = userId.ToString();
        return properties;
    }

    /// <summary>The account a connect challenge attaches its provider to; null for an ordinary sign-in or unreadable state.</summary>
    internal static Guid? FindAccountToConnect(this AuthenticationProperties? properties)
        => properties is not null && properties.Items.TryGetValue(AccountToConnectKey, out var id) && Guid.TryParse(id, out var userId)
            ? userId
            : null;
}
