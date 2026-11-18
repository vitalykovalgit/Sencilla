# Sencilla.Authentication.Google

Google sign-in for the Sencilla authentication family, in two shapes that resolve accounts the same way:

- **Token exchange** (always on): an `IProviderTokenVerifier` that validates a Google `id_token` against
  Google's JWKS and the configured audiences, for `POST {BasePath}/external` and native apps.
- **Redirect sign-in** (`RedirectSignIn = true`): ASP.NET's Google handler on `/signin-google`, signing into
  the host's default sign-in scheme (usually its cookie). The library owns the handler's events.

```csharp
services.AddSencillaAuthentication(configuration, c => c
    .UseCookieSession()
    .UseGoogle(o => { o.RedirectSignIn = true; o.SignInFailedUrl = "https://example.com/login"; }));
```

```jsonc
"Authentication": {
  "Google": {
    "ClientId": "<web-client-id>",          // folded into Audiences; the redirect handler's client id
    "ClientSecret": "<web-client-secret>",  // redirect sign-in only
    "Audiences": [ "<ios-client-id>" ]      // extra id_token audiences (native apps)
  }
}
```

The section binds first, then the `UseGoogle` lambda overrides it. `RedirectSignIn` without a `ClientId`
and a `ClientSecret` fails at startup.

## Which account a Google sign-in becomes

Both shapes go through `IExternalAuthService` and `IAccountLinkingPolicy`:

1. A Google subject already linked → that account (refused if it is locked or disabled).
2. An address nobody owns → a new account.
3. An address an account owns, and Google says it is **verified** → the login is linked to that account. A
   password credential on it whose address was never confirmed is removed (`PasswordChanged`, reason
   `removed`): the provider proved who owns the address, so whoever registered that password did not.
4. An address an account owns, but Google says it is **not verified** → refused (`email-not-verified`).

On the redirect flow the session carries the account's id as `sub` (not Google's subject), the account's
email, and is persistent. A refusal never creates a session: it is thrown from the ticket event and lands on
`{SignInFailedUrl}?error={code}` — the refusal code (`account-not-active`, `email-not-verified`) or
`google-failed` for anything else. Without `SignInFailedUrl` the failure surfaces as the framework's error.

## License

MIT
