namespace Sencilla.Authentication;

/// <summary>
/// The single fluent entry point for the authentication family. Each <c>Use*</c> plugs a
/// component (issuer, password accounts, social provider, user store); <c>AcceptBearer</c> declares
/// what tokens this app validates; <c>UseApi</c> governs the built-in HTTP endpoints. Calls only
/// <em>record</em> intent into options objects (bound from configuration when first created, then
/// handed to the caller's lambda) — <see cref="Build"/> runs once at the end to apply defaults, reuse
/// the shared signing key, validate the combination, and register everything (so call order is
/// irrelevant). <see cref="Services"/> and <see cref="Configuration"/> are exposed as an escape
/// hatch for custom <c>Use*</c> extensions. This part records; <c>AuthenticationConfig.Build.cs</c> registers.
/// </summary>
public sealed partial class AuthenticationConfig
{
    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }

    /// <summary>True when <c>ASPNETCORE_ENVIRONMENT</c> is <c>Development</c>.</summary>
    public bool IsDevelopment { get; }

    internal JwtIssuerOptions? Issuer { get; private set; }
    internal AppAccountsOptions? AppAccounts { get; private set; }
    internal List<ProviderRegistration> Providers { get; } = [];
    internal List<RedirectSignInRegistration> RedirectSignIns { get; } = [];
    internal Action<IServiceCollection>? UserStore { get; set; }
    internal BearerOptions? Bearer { get; private set; }
    internal CookieSessionOptions? CookieSession { get; private set; }
    internal AuthApiOptions Api { get; } = new();

    internal AuthenticationConfig(IServiceCollection services, IConfiguration configuration)
    {
        Services = services;
        Configuration = configuration;
        IsDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);
    }

    // ── recording seams used by the Use*/Accept* extensions (same assembly) ──────────────────

    // Configuration binds when the options are created; the caller's lambda, run right after, overrides it.
    internal JwtIssuerOptions GetOrAddIssuer() => Issuer ??= Configuration.GetSection("Jwt").Get<JwtIssuerOptions>() ?? new();
    internal BearerOptions GetOrAddBearer() => Bearer ??= new BearerOptions();
    internal CookieSessionOptions GetOrAddCookieSession() => CookieSession ??= new CookieSessionOptions();

    internal AppAccountsOptions GetOrAddAppAccounts()
    {
        if (AppAccounts is not null)
            return AppAccounts;
        AppAccounts = new AppAccountsOptions();
        Configuration.GetSection("Authentication:Password").Bind(AppAccounts.Password);
        return AppAccounts;
    }

    /// <summary>
    /// Register a social provider verifier and bind its options from <paramref name="section"/>.
    /// Used by the provider packages' <c>UseGoogle</c>/<c>UseFacebook</c>/<c>UseApple</c> extensions
    /// and by any third-party provider — the one extension point needed to add a login provider.
    /// </summary>
    public AuthenticationConfig AddProvider<TVerifier, TOptions>(string provider, string section, Action<TOptions>? configure = null)
        where TVerifier : class, IProviderTokenVerifier
        where TOptions : ProviderVerifierOptions, new()
    {
        Providers.Add(new ProviderRegistration(provider, () =>
        {
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IProviderTokenVerifier, TVerifier>());

            var options = Services.AddOptions<TOptions>();
            var configSection = Configuration.GetSection(section);
            if (configSection.Exists())
                options.Bind(configSection);
            if (configure is not null)
                options.Configure(configure);

            // Fold the convenience single ClientId / AppId into the accepted audiences, so a host
            // may configure either a single id or an explicit Audiences array (as before).
            options.PostConfigure(o =>
            {
                foreach (var id in new[] { o.ClientId, o.AppId })
                    if (!string.IsNullOrWhiteSpace(id) && !o.Audiences.Contains(id))
                        o.Audiences.Add(id);
            });
        }));
        return this;
    }

    /// <summary>
    /// Read a provider's redirect sign-in from <paramref name="section"/>, then <paramref name="configure"/>: null while
    /// its <see cref="RedirectSignInOptions.RedirectSignIn"/> is off, otherwise the options the provider registers its
    /// handler from — after refusing at startup while a setting it needs is missing. Records the provider for
    /// <c>GET {BasePath}/providers</c> and binds the <paramref name="scheme"/> handler's events to the account graph
    /// (<see cref="RedirectSignInBinding"/>): a provider supplies only <paramref name="readIdentity"/>, how to read its
    /// answer — the handler's principal — into an <see cref="ExternalIdentity"/>. The one extension point a redirect
    /// provider needs, as <see cref="AddProvider{TVerifier, TOptions}"/> is for a verifier.
    /// </summary>
    public TOptions? AddRedirectSignIn<TOptions, THandlerOptions>(
        string provider, string scheme, string section, Action<TOptions>? configure, Func<ClaimsPrincipal, ExternalIdentity> readIdentity)
        where TOptions : RedirectSignInOptions, new()
        where THandlerOptions : RemoteAuthenticationOptions
    {
        var options = new TOptions();
        Configuration.GetSection(section).Bind(options);
        configure?.Invoke(options);
        if (!options.RedirectSignIn)
            return null;

        var missing = options.FindMissingRedirectSettings().ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"{scheme} RedirectSignIn needs {string.Join(", ", missing)} ({section} or the Use{scheme} lambda).");

        // A post-configure: the library owns these two events, whatever the host's own configure set. Registered at
        // Build, after the provider's Add*, whose TryAdd of its own post-configure (Apple's) would otherwise see this
        // one and skip.
        RedirectSignIns.Add(new RedirectSignInRegistration(new RedirectSignInProvider(provider, scheme), () => Services
            .AddOptions<THandlerOptions>(scheme)
            .PostConfigure(handler => RedirectSignInBinding.Bind(handler, provider, options.SignInFailedUrl, readIdentity))));
        return options;
    }
}

/// <summary>A recorded social-provider registration (verifier + options binding).</summary>
internal sealed record ProviderRegistration(string Provider, Action Register);

/// <summary>A recorded redirect sign-in: what <c>/providers</c> lists, and the binding of its handler's events.</summary>
internal sealed record RedirectSignInRegistration(RedirectSignInProvider Provider, Action Bind);
