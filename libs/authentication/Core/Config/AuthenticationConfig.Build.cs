namespace Sencilla.Authentication;

/// <summary>The materialization of what the <c>Use*</c> calls recorded: <see cref="Build"/> registers it all, once, in dependency order.</summary>
public sealed partial class AuthenticationConfig
{
    // A clearly-marked development fallback so a dev host without a configured key still boots.
    private const string DevSigningKey = "sencilla-dev-only-signing-key-change-me-please!!";

    /// <summary>Any way to sign in: password accounts, a provider's token exchange or a redirect sign-in.</summary>
    private bool HasSignIns => AppAccounts is not null || Providers.Count > 0 || RedirectSignIns.Count > 0;

    internal void Build()
    {
        if (Issuer is not null && CookieSession is not null)
            throw new InvalidOperationException(
                "UseCookieSession and UseJwtToken are mutually exclusive: an app issues its sessions through one transport, a cookie or a token pair.");

        if (!Api.Disabled && (AppAccounts is not null || Providers.Count > 0) && Issuer is null && CookieSession is null)
            throw new InvalidOperationException(
                "The built-in sign-in endpoints need a session transport: call UseJwtToken() or UseCookieSession(), "
                + "or disable the built-in endpoints with UseApi(o => o.Disabled = true).");

        BuildIssuer();
        BuildCookieSession();
        BuildAppAccounts();
        BuildProviders();
        UserStore?.Invoke(Services);
        BuildLinking();
        BuildBearer();
        BuildEndpoints();
    }

    private void BuildCookieSession()
    {
        if (CookieSession is null)
            return;

        var options = CookieSession;

        Services.AddSingleton<IOptions<CookieSessionOptions>>(Microsoft.Extensions.Options.Options.Create(options));
        Services.AddTransient<ISessionTransport, CookieSessionTransport>();
        Services.TryAddTransient<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();

        // Chain onto the host's own cookie events (its AddCookie(...) configure runs before PostConfigure):
        // every sign-in stamps when its session starts (and counts it validated then), and a disabled account
        // or a credential change after that start ends the session at its next check.
        Services.AddOptions<CookieAuthenticationOptions>(options.Scheme).PostConfigure(cookie =>
        {
            var previousSigningIn = cookie.Events.OnSigningIn;
            cookie.Events.OnSigningIn = async context =>
            {
                await previousSigningIn(context);
                CookieSessionValidator.RecordSessionStartAsValidated(context.Properties);
            };

            var previousValidatePrincipal = cookie.Events.OnValidatePrincipal;
            cookie.Events.OnValidatePrincipal = async context =>
            {
                await previousValidatePrincipal(context);
                if (context.Principal is not null)
                    await CookieSessionValidator.Validate(context, options);
            };
        });
    }

    private void BuildLinking()
    {
        if (!HasSignIns && CookieSession is null)
            return;

        Services.TryAddTransient<IAccountLinkService, AccountLinkService>();
    }

    private void BuildIssuer()
    {
        if (Issuer is null)
            return;

        // Applied before BuildBearer, which reuses the symmetric key.
        if (string.IsNullOrEmpty(Issuer.SigningKey) && IsDevelopment)
            Issuer.SigningKey = DevSigningKey;

        Services.AddSingleton<IOptions<JwtIssuerOptions>>(Microsoft.Extensions.Options.Options.Create(Issuer));
        Services.AddSingleton<IOptions<RefreshTokenOptions>>(Microsoft.Extensions.Options.Options.Create(Issuer.Refresh));
        Services.AddTransient<ITokenIssuer, JwtTokenIssuer>();
        Services.AddTransient<ISessionTransport, JwtSessionTransport>();
        Services.AddTransient<RefreshTokenService>();
        Services.TryAddTransient<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();

        if (Issuer.Refresh.Storage == RefreshStorage.EntityFramework)
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
    }

    private void BuildAppAccounts()
    {
        if (AppAccounts is null)
            return;

        if (UserStore is null)
            throw new InvalidOperationException(
                "UseAppAccounts requires a user store. Call UseUsers(u => u.EntityFrameworkStore()) (or a custom store).");

        Services.AddSingleton<IOptions<AppAccountsOptions>>(Microsoft.Extensions.Options.Options.Create(AppAccounts));
        Services.AddSingleton<IOptions<PasswordPolicyOptions>>(Microsoft.Extensions.Options.Options.Create(AppAccounts.Password));
        Services.AddSingleton<IOptions<LockoutOptions>>(Microsoft.Extensions.Options.Options.Create(AppAccounts.Lockout));

        Services.TryAddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        Services.TryAddTransient<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();
        Services.TryAddTransient<ICredentialAuthService, CredentialAuthService>();
        Services.TryAddTransient<IAccountRecoveryService, AccountRecoveryService>();

        // PasswordResetToken needs its EF repository wired just like RefreshTokenEntity.
        Services.RegisterEFRepositoriesForType(typeof(PasswordResetToken), out _);
        if (!RepositoryEntityFrameworkBootstrap.Entities.Contains(typeof(PasswordResetToken)))
            RepositoryEntityFrameworkBootstrap.Entities.Add(typeof(PasswordResetToken));

        if (AppAccounts.DefaultRoles.Count > 0)
            Services.AddTransient<IEventHandlerBase<UserRegistered>, DefaultRoleAssigner>();
    }

    private void BuildProviders()
    {
        if (Providers.Count == 0 && RedirectSignIns.Count == 0)
            return;

        foreach (var provider in Providers)
            provider.Register();
        foreach (var redirect in RedirectSignIns)
        {
            Services.AddSingleton(redirect.Provider);
            redirect.Bind();
        }

        Services.TryAddTransient<IAccountLinkingPolicy, AccountLinkingPolicy>();
        Services.TryAddTransient<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();
        Services.TryAddTransient<IExternalAuthService, ExternalAuthService>();
    }

    private void BuildBearer()
    {
        if (Bearer is null)
            return;

        var authority = Bearer.Authority;
        var metadata = Bearer.MetadataAddress;
        var signingKey = Bearer.SigningKey ?? Issuer?.SigningKey;
        var issuer = Bearer.Issuer ?? Issuer?.Issuer;
        var audiences = Bearer.Audiences.Count > 0
            ? Bearer.Audiences.ToList()
            : Issuer?.Audience is { Length: > 0 } aud ? [aud] : [];
        var requireHttps = Bearer.RequireHttps ?? !IsDevelopment;

        if (string.IsNullOrEmpty(authority) && string.IsNullOrEmpty(metadata) && string.IsNullOrEmpty(signingKey))
            throw new InvalidOperationException(
                "AcceptBearer needs an Authority/MetadataAddress (external IDP) or a signing key. "
                + "Call UseJwtToken(...) to issue+validate embedded tokens, or AcceptBearer(b => b.SigningKey = ... / b.Authority = ...).");

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
        if (Api.Disabled)
            return;

        Services.AddSingleton<IOptions<AuthApiOptions>>(Microsoft.Extensions.Options.Options.Create(Api));
        Services.TryAddSingleton<AuthRateLimiter>();

        if (AppAccounts is not null)
        {
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, AppAccountsEndpoint>());
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, RecoveryEndpoint>());
        }
        if (Issuer is not null)
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, SessionEndpoint>());
        if (Providers.Count > 0)
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, ExternalEndpoint>());
        if (CookieSession is not null)
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, CookieSessionEndpoint>());
        if ((Bearer is not null || CookieSession is not null) && Api.MeEnabled)
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, MeEndpoint>());
        if (HasSignIns)
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, ProvidersEndpoint>());
        // The account's own ways in need a session to know whose they are.
        if (HasSignIns && (Bearer is not null || CookieSession is not null))
            Services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, LoginsEndpoint>());
    }
}
