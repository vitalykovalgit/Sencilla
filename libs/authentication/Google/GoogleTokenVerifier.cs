namespace Sencilla.Authentication;

/// <summary>Verifies Google <c>id_token</c>s and maps them to an <see cref="ExternalIdentity"/>.</summary>
public sealed class GoogleTokenVerifier(IOptions<GoogleProviderOptions> options)
    : ProviderTokenVerifierBase<GoogleProviderOptions>(options, "google");
