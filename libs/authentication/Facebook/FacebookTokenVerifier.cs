namespace Sencilla.Authentication;

/// <summary>
/// Verifies Facebook Limited Login <c>id_token</c>s and maps them to an <see cref="ExternalIdentity"/>. Facebook's
/// address is never verified, whatever the token claims — the same rule as its redirect sign-in.
/// </summary>
public sealed class FacebookTokenVerifier(IOptions<FacebookProviderOptions> options)
    : ProviderTokenVerifierBase<FacebookProviderOptions>(options, "facebook")
{
    protected override ExternalIdentity Map(JsonWebToken token) => base.Map(token) with { EmailVerified = false };
}
