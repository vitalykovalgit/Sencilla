namespace Sencilla.Authentication;

/// <summary>
/// The single fluent entry point for the authentication family. Each <c>Use*</c> plugs a
/// component (issuer, password accounts, social provider, user store); <c>AcceptBearer</c> declares
/// what tokens this app validates; <c>UseApi</c> governs the built-in HTTP endpoints. Calls only
/// <em>record</em> intent — <see cref="Build"/> runs once at the end to apply defaults, reuse the
/// shared signing key, validate the combination, and register everything (so call order is
/// irrelevant). <see cref="Services"/> and <see cref="Configuration"/> are exposed as an escape
/// hatch for custom <c>Use*</c> extensions.
/// </summary>
public sealed class AuthenticationConfig
{
    // A clearly-marked development fallback so a dev host without a configured key still boots.
    private const string DevSigningKey = "sencilla-dev-only-signing-key-change-me-please!!";

    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }

    /// <summary>True when <c>ASPNETCORE_ENVIRONMENT</c> is <c>Development</c>.</summary>
    public bool IsDevelopment { get; }

    internal JwtTokenConfig? Issuer { get; private set; }
    internal AppAccountsConfig? AppAccounts { get; private set; }
    internal List<ProviderRegistration> Providers { get; } = [];
    internal Action<IServiceCollection>? UserStore { get; set; }
    internal BearerConfig? Bearer { get; private set; }
    internal AuthApiConfig Api { get; } = new();

    internal AuthenticationConfig(IServiceCollection services, IConfiguration configuration)
    {
        Services = services;
        Configuration = configuration;
        IsDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);
    }

    // ── recording seams used by the Use*/Accept* extensions (same assembly) ──────────────────

    internal JwtTokenConfig GetOrAddIssuer() => Issuer ??= new JwtTokenConfig();
    internal AppAccountsConfig GetOrAddAppAccounts() => AppAccounts ??= new AppAccountsConfig();
    internal BearerConfig GetOrAddBearer() => Bearer ??= new BearerConfig();

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

    // ── materialization ──────────────────────────────────────────────────────────────────────

    internal void Build()
    {
        var issuerOptions = BuildIssuer();
        BuildAppAccounts();
        BuildProviders();
        UserStore?.Invoke(Services);
        BuildBearer(issuerOptions);
        BuildEndpoints();
    }

    private JwtIssuerOptions? BuildIssuer()
    {
        if (Issuer is null)
            return null;

        // Resolve effective issuer options eagerly so AcceptBearer can reuse the symmetric key.
        var options = new JwtIssuerOptions();
        var section = Configuration.GetSection("Jwt");
        if (section.Exists())
            section.Bind(options);
        Issuer.OverrideOptions?.Invoke(options);

        if (string.IsNullOrEmpty(options.SigningKey) && IsDevelopment)
            options.SigningKey = DevSigningKey;

        Services.AddSingleton<IOptions<JwtIssuerOptions>>(Microsoft.Extensions.Options.Options.Create(options));
        Services.AddTransient<ITokenIssuer, JwtTokenIssuer>();
        Services.AddTransient<RefreshTokenService>();
        Services.TryAddTransient<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();

        var refresh = Issuer.RefreshConfig;
        Services.Configure<RefreshTokenOptions>(o =>
        {
            if (refresh.LifetimeDaysValue is { } days) o.LifetimeDays = days;
            if (refresh.RotateValue is { } rotate) o.Rotate = rotate;
            if (refresh.DetectReuseValue is { } detect) o.DetectReuse = detect;
        });

        if (refresh.Storage == RefreshStorage.EntityFramework)
        {
            // Make the EF repository layer aware of RefreshTokenEntity so DbRefreshTokenStore can
            // resolve its ICreate/IUpdate repositories — independent of whether the host called
            // AddSencillaRepositoryForEF before or after us. We register the CRUD repos against the
            // dynamic context and add the entity to the model so sec.RefreshToken is mapped.
            Services.RegisterEFRepositoriesForType(typeof(RefreshTokenEntity), out _);
            if (!RepositoryEntityFrameworkBootstrap.Entities.Contains(typeof(RefreshTokenEntity)))
                RepositoryEntityFrameworkBootstrap.Entities.Add(typeof(RefreshTokenEntity));

            Services.AddTransient<IRefreshTokenStore, DbRefreshTokenStore>();
        }
        else
            Services.TryAddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();

        return options;
    }

    private void BuildAppAccounts()
    {
        if (AppAccounts is null)
            return;

        if (UserStore is null)
            throw new InvalidOperationException(
                "UseAppAccounts requires a user store. Call UseUsers(u => u.EntityFrameworkStore()) (or a custom store).");

        var section = Configuration.GetSection("Authentication:Password");
        Services.Configure<PasswordPolicyOptions>(o =>
        {
            if (section.Exists()) section.Bind(o);
            AppAccounts.PasswordOverride?.Invoke(o);
        });
        Services.Configure<LockoutOptions>(o => AppAccounts.LockoutOverride?.Invoke(o));
        Services.Configure<AppAccountsOptions>(o =>
        {
            o.PhoneLoginEnabled = AppAccounts.PhoneLogin;
            o.DefaultRoles = AppAccounts.DefaultRoles.ToList();
        });

        Services.TryAddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        Services.TryAddTransient<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();
        Services.TryAddTransient<ICredentialAuthService, CredentialAuthService>();

        if (AppAccounts.DefaultRoles.Count > 0)
            Services.AddTransient<IEventHandlerBase<UserRegistered>, DefaultRoleAssigner>();
    }

    private void BuildProviders()
    {
        if (Providers.Count == 0)
            return;

        foreach (var provider in Providers)
            provider.Register();

        Services.TryAddTransient<IAccountLinkingPolicy, AccountLinkingPolicy>();
        Services.TryAddTransient<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();
        Services.TryAddTransient<IExternalAuthService, ExternalAuthService>();
    }

    private void BuildBearer(JwtIssuerOptions? issuerOptions)
    {
        if (Bearer is null)
            return;

        var authority = Bearer.AuthorityValue;
        var metadata = Bearer.MetadataAddressValue;
        var signingKey = Bearer.SigningKeyValue ?? issuerOptions?.SigningKey;
        var issuer = Bearer.IssuerValue ?? issuerOptions?.Issuer;
        var audiences = Bearer.AudiencesValue.Count > 0
            ? Bearer.AudiencesValue.ToList()
            : issuerOptions?.Audience is { Length: > 0 } aud ? [aud] : [];
        var requireHttps = Bearer.RequireHttpsValue ?? !IsDevelopment;

        if (string.IsNullOrEmpty(authority) && string.IsNullOrEmpty(metadata) && string.IsNullOrEmpty(signingKey))
            throw new InvalidOperationException(
                "AcceptBearer needs an Authority/MetadataAddress (external IDP) or a signing key. "
                + "Call UseJwtToken(...) to issue+validate embedded tokens, or AcceptBearer(b => b.SigningKey(...)/b.Authority(...)).");

        Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.RequireHttpsMetadata = requireHttps;

                if (!string.IsNullOrEmpty(authority))
                    jwt.Authority = authority;
                if (!string.IsNullOrEmpty(metadata))
                    jwt.MetadataAddress = metadata;

                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = audiences.Count > 0,
                    ValidAudiences = audiences,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = AuthClaims.Subject,
                };

                if (!string.IsNullOrEmpty(signingKey))
                    jwt.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            });
    }

    private void BuildEndpoints()
    {
        if (Api.Options.Disabled)
            return;

        Services.AddSingleton<IOptions<AuthApiOptions>>(Microsoft.Extensions.Options.Options.Create(Api.Options));

        if (AppAccounts is not null)
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, AppAccountsEndpoint>());
        if (Issuer is not null)
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, SessionEndpoint>());
        if (Providers.Count > 0)
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, ExternalEndpoint>());
        if (Bearer is not null && Api.Options.MeEnabled)
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, MeEndpoint>());
    }
}

/// <summary>A recorded social-provider registration (verifier + options binding).</summary>
internal sealed record ProviderRegistration(string Provider, Action Register);
