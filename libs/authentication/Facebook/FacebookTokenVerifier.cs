namespace Sencilla.Authentication;

/// <summary>Verifies Facebook Limited Login <c>id_token</c>s and maps them to an <see cref="ExternalIdentity"/>.</summary>
public sealed class FacebookTokenVerifier(IOptions<FacebookProviderOptions> options)
    : ProviderTokenVerifierBase<FacebookProviderOptions>(options, "facebook");
