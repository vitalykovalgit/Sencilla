# Sencilla.Authentication.Facebook

Facebook sign-in for the Sencilla authentication family, in two shapes that resolve accounts the same way:

- **Token exchange** (always on): an `IProviderTokenVerifier` for Facebook **Limited Login** `id_token`s, validated
  against Facebook's JWKS and the configured audiences (app ids), for `POST {BasePath}/external` and native apps.
- **Redirect sign-in** (`RedirectSignIn = true`): ASP.NET's Facebook handler, scheme `Facebook`, on
  `/signin-facebook`, signing into the host's default sign-in scheme. The library owns the handler's events — the
  shared binding of `Sencilla.Authentication` (its README, «Redirect sign-in»), connect included.

```csharp
services.AddSencillaAuthentication(configuration, c => c
    .UseCookieSession()
    .UseFacebook(o => { o.RedirectSignIn = true; o.SignInFailedUrl = "https://example.com/login"; }));
```

```jsonc
"Authentication": {
  "Facebook": {
    "ClientId": "<App ID>",          // the redirect handler's app; folded into Audiences
    "ClientSecret": "<App Secret>",  // redirect sign-in only
    "RedirectSignIn": true,
    "SignInFailedUrl": "https://example.com/login"
  }
}
```

`RedirectSignIn` without a `ClientId` and a `ClientSecret` fails at startup. Register
`https://<domain>/signin-facebook` as a valid OAuth redirect URI of the app (Facebook Login → Settings).

## The identity

The redirect sign-in reads the Graph `me` answer with the fields `name, email, first_name, last_name, picture`:
`id` → `Subject`, `email`, `name`, `first_name` / `last_name` → `GivenName` / `FamilyName`, `picture.data.url` →
`Picture`.

**`EmailVerified` is always false, on both shapes** — the Limited Login verifier ignores an `email_verified` claim
too: Facebook gives no verified flag, and an account registered by phone has no address at all. So a Facebook address
never links to an existing account (one that already has it is refused, `email-not-verified`), and a new account
starts without it — with app accounts on, our own link goes to it as a pending address, which reaches the account
only when followed.

## Unverified

The redirect half is ASP.NET's stock Facebook handler; nothing beyond its defaults was checked against a real app. The
`picture` field shape (`{ "data": { "url" } }`) is the Graph API's documented one.

## License

MIT
