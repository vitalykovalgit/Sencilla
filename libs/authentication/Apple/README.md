# Sencilla.Authentication.Apple

Sign in with Apple for the Sencilla authentication family, in two shapes that resolve accounts the same way:

- **Token exchange** (always on): an `IProviderTokenVerifier` that validates an Apple `id_token` against Apple's JWKS
  and the configured audience set (Services ID / bundle ids), for `POST {BasePath}/external` and native apps.
- **Redirect sign-in** (`RedirectSignIn = true`): the `AspNet.Security.OAuth.Apple` handler, scheme `Apple`, on
  `/signin-apple`, signing into the host's default sign-in scheme. The library owns the handler's events — the shared
  binding of `Sencilla.Authentication` (its README, «Redirect sign-in»), connect included.

```csharp
services.AddSencillaAuthentication(configuration, c => c
    .UseCookieSession()
    .UseApple(o => { o.RedirectSignIn = true; o.SignInFailedUrl = "https://example.com/login"; }));
```

```jsonc
"Authentication": {
  "Apple": {
    "ClientId": "<Services ID>",     // e.g. pro.example.signin; folded into Audiences
    "TeamId": "<Team ID>",
    "KeyId": "<Sign in with Apple key id>",
    "PrivateKey": "-----BEGIN PRIVATE KEY-----\n…\n-----END PRIVATE KEY-----",  // the .p8 file's CONTENT, from secrets
    "RedirectSignIn": true,
    "SignInFailedUrl": "https://example.com/login"
  }
}
```

There is no `ClientSecret`: the handler signs one (ES256, from the team, about the Services ID, under the key id) with
`PrivateKey`, whose content comes from the deployment's secrets — never a file in the repository. A one-line env file
cannot keep the .p8's line breaks, so `PrivateKey` is accepted in each shape such a value takes:

- the PEM as it is, with real line breaks;
- the PEM with literal `\n` for its line breaks (`-----BEGIN PRIVATE KEY-----\nMIGT…\n-----END PRIVATE KEY-----`);
- the bare base64 body, without the BEGIN / END lines.

`RedirectSignIn` without `ClientId`, `TeamId`, `KeyId` and `PrivateKey` fails at startup. Register
`https://<domain>/signin-apple` as a return URL of the Services ID.

## The posted answer

Apple POSTs its answer back to `/signin-apple` (`response_mode=form_post`), a cross-site request. The correlation
cookie the challenge sets is `SameSite=None; Secure` (ASP.NET's default for remote handlers), so it travels on that
POST over HTTPS; a connect's intent rides the protected `state`, not the session cookie.

## The identity

| `ExternalIdentity` | from |
| --- | --- |
| `Subject` | the `id_token`'s `sub` |
| `Email`, `EmailVerified` | `email`, `email_verified` (Apple sends it as a string) |
| `RawClaims["is_private_email"]` | `"true"` for a private relay address (`@privaterelay.appleid.com`) |
| `GivenName`, `FamilyName` | the posted `user` field's `name.firstName` / `name.lastName` — **on the first sign-in only** |

A relay address is a verified address: it starts a new account confirmed, and can never match an older one. Mail to a
relay address is delivered only from a sending domain registered with Apple. The name is never in the `id_token`;
Apple posts it once, typed by the user, so it is only ever their own account's name.

## Unverified

No real sign-in was tried. Everything is from `AspNet.Security.OAuth.Apple` 10.0.0's behaviour, Apple's discovery
document (`token_endpoint_auth_methods_supported: client_secret_post`, `response_modes_supported: form_post`) and
Apple's «Incorporating Sign in with Apple into other platforms» page for the posted `user` field
(`{ "name": { "firstName", "lastName" }, "email" }`).

## License

MIT
