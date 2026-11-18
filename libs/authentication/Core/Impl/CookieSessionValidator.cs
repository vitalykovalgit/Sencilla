using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Sencilla.Authentication;

/// <summary>
/// Re-checks a cookie principal against the user store at most once per
/// <see cref="CookieSessionOptions.ValidationInterval"/>: a disabled account, or a password set /
/// reset / removed AFTER the session started, ends the session. Only principals that carry a user id
/// are checked. A ticket issued before the id was in the session (an email, no id) is upgraded once to
/// the account's own principal, or ends when no usable account owns that email; any other identity is
/// left alone.
/// </summary>
public static class CookieSessionValidator
{
    private const string ValidatedAt = "sencilla.validated";
    private const string StartedAt = "sencilla.started";

    /// <summary>
    /// Stamps the moment a session starts into its ticket, to the tick. Run on every sign-in to the cookie
    /// scheme and never on a renewal, so — unlike <see cref="AuthenticationProperties.IssuedUtc"/>, which keeps
    /// whole seconds and moves forward with every renewal — it stays the session's true start. The same moment
    /// is its first validation: the sign-in built it from a user read a moment ago, so the first re-check comes
    /// one <see cref="CookieSessionOptions.ValidationInterval"/> later, and the parallel requests of the first
    /// page after a sign-in neither re-read the user nor re-issue the cookie.
    /// </summary>
    internal static void RecordSessionStartAsValidated(AuthenticationProperties properties)
        => properties.Items[StartedAt] = properties.Items[ValidatedAt] = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);

    public static async Task Validate(CookieValidatePrincipalContext context, CookieSessionOptions options)
    {
        if (context.Principal is null)
            return;

        if (context.Principal.GetUserId() is not { } userId)
        {
            // ponytail: exists only for tickets issued before the account id was in the session; delete it
            // one ticket lifetime (30 days) after the release that ships it.
            if (context.Principal.FindFirst(ClaimTypes.Email)?.Value is { Length: > 0 } email)
            {
                var services = context.HttpContext.RequestServices;
                var owner = await services.GetRequiredService<IUserStore>().FindByEmail(email, context.HttpContext.RequestAborted);
                if (owner is null || owner.Status is AuthUserStatus.Disabled or AuthUserStatus.Locked)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(options.Scheme);
                    return;
                }

                context.ReplacePrincipal(await services.GetRequiredService<IClaimsPrincipalFactory>().Create(owner, scopes: null, context.HttpContext.RequestAborted));
                context.Properties.Items[ValidatedAt] = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                context.ShouldRenew = true;
            }
            return;
        }

        if (ReadStampedTime(context.Properties, ValidatedAt) is { } validated
            && DateTimeOffset.UtcNow - validated < options.ValidationInterval)
            return;

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserStore>();
        var user = await users.FindById(userId, context.HttpContext.RequestAborted);

        if (user is null || user.Status is AuthUserStatus.Disabled || StartedBeforeCredentialChange(SessionStartedAt(context.Properties), user))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(options.Scheme);
            return;
        }

        context.Properties.Items[ValidatedAt] = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        context.ShouldRenew = true;
    }

    /// <summary>
    /// When the session started: the stamp of its sign-in, or — for a ticket issued before the stamp
    /// existed — its <see cref="AuthenticationProperties.IssuedUtc"/>.
    /// </summary>
    private static DateTimeOffset? SessionStartedAt(AuthenticationProperties properties)
        => ReadStampedTime(properties, StartedAt) ?? properties.IssuedUtc;

    private static DateTimeOffset? ReadStampedTime(AuthenticationProperties properties, string key)
        => properties.Items.TryGetValue(key, out var stamp)
           && DateTimeOffset.TryParse(stamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
            ? at
            : null;

    private static bool StartedBeforeCredentialChange(DateTimeOffset? started, AuthUser user)
    {
        if (started is null || user.CredentialsChangedAt is not { } changed)
            return false;
        var changedUtc = DateTime.SpecifyKind(changed, DateTimeKind.Utc);
        return changedUtc > started.Value.UtcDateTime;
    }
}
