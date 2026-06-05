namespace Sencilla.Authentication;

/// <summary>
/// Shared, security-critical <c>id_token</c> validation for external providers. A subclass only
/// supplies its options type and provider name; the base validates signature, issuer, audience and
/// lifetime against the provider's (cached, auto-rotating) JWKS and maps the standard OIDC claims.
/// Providers with non-standard claims (e.g. Apple) override <see cref="Map"/>.
/// </summary>
public abstract class ProviderTokenVerifierBase<TOptions> : IProviderTokenVerifier
    where TOptions : ProviderVerifierOptions
{
    private static readonly JsonWebTokenHandler Handler = new();
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configuration;

    protected TOptions Options { get; }

    public string Provider { get; }

    protected ProviderTokenVerifierBase(IOptions<TOptions> options, string provider)
    {
        Options = options.Value;
        Provider = provider;
        _configuration = Options.ConfigurationManager
            ?? new ConfigurationManager<OpenIdConnectConfiguration>(
                Options.MetadataAddress, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever());
    }

    /// <summary>Maps a validated token to an <see cref="ExternalIdentity"/> using the standard OIDC claims.</summary>
    protected virtual ExternalIdentity Map(JsonWebToken token) => new(
        Provider: Provider,
        Subject: token.Subject,
        Email: ReadString(token, "email"),
        EmailVerified: ReadBool(token, "email_verified"),
        Name: ReadString(token, "name"),
        Picture: ReadString(token, "picture"));

    public async Task<ExternalIdentity> Verify(string idToken, CancellationToken token = default)
    {
        OpenIdConnectConfiguration config;
        try
        {
            config = await _configuration.GetConfigurationAsync(token);
        }
        catch (Exception ex)
        {
            throw new UnauthorizedException($"Unable to load {Provider} metadata: {ex.Message}");
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = Options.Issuers ?? (config.Issuer is { Length: > 0 } iss ? new[] { iss } : null),
            ValidateAudience = true,
            ValidAudiences = Options.Audiences,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,
        };

        var result = await Handler.ValidateTokenAsync(idToken, parameters);
        if (!result.IsValid)
            throw new UnauthorizedException($"Invalid {Provider} token: {result.Exception?.Message}");

        return Map((JsonWebToken)result.SecurityToken);
    }

    /// <summary>Reads a claim that providers send inconsistently as a JSON bool or a string.</summary>
    protected static bool ReadBool(JsonWebToken token, string claim)
    {
        if (token.TryGetPayloadValue<bool>(claim, out var b))
            return b;
        if (token.TryGetPayloadValue<string>(claim, out var s) && bool.TryParse(s, out var parsed))
            return parsed;
        return false;
    }

    protected static string? ReadString(JsonWebToken token, string claim)
        => token.TryGetPayloadValue<string>(claim, out var s) ? s : null;
}
