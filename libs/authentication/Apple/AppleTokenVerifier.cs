namespace Sencilla.Authentication;

/// <summary>Verifies Apple <c>id_token</c>s and maps them to an <see cref="ExternalIdentity"/>.</summary>
public sealed class AppleTokenVerifier(IOptions<AppleProviderOptions> options)
    : ProviderTokenVerifierBase<AppleProviderOptions>(options, "apple")
{
    protected override ExternalIdentity Map(JsonWebToken token)
    {
        // Apple sends email_verified / is_private_email as strings; the name is never in the token.
        Dictionary<string, string>? raw = null;
        var isPrivate = ReadBool(token, "is_private_email");
        if (isPrivate)
            raw = new Dictionary<string, string> { ["is_private_email"] = "true" };

        return new ExternalIdentity(
            Provider: Provider,
            Subject: token.Subject,
            Email: ReadString(token, "email"),
            EmailVerified: ReadBool(token, "email_verified"),
            Name: null,
            Picture: null,
            RawClaims: raw);
    }
}
