# Sencilla.Authentication.Telegram

Log In With Telegram for the Sencilla authentication family, as a redirect sign-in on ASP.NET's generic OpenID
Connect handler: authorization code + PKCE, the claims read from the `id_token` alone (Telegram has no user-info
endpoint). The library owns the handler's events; the sign-in resolves through `IExternalAuthService` like every
other provider (see `Sencilla.Authentication`'s README, «Redirect sign-in»).

```csharp
services.AddSencillaAuthentication(configuration, c => c
    .UseCookieSession()
    .UseTelegram(o => o.SignInFailedUrl = "https://example.com/login"));
```

```jsonc
"Authentication": {
  "Telegram": {
    "ClientId": "<bot id>",          // @BotFather → the bot → Login Widget → OpenID Connect
    "ClientSecret": "<client secret>",
    "SignInFailedUrl": "https://example.com/login"
  }
}
```

`RedirectSignIn` starts on (it is Telegram's only shape), so `UseTelegram` without a `ClientId` and a `ClientSecret`
fails at startup — a host with no keys in a deployment does not call it.

## What it registers

| | |
| --- | --- |
| Scheme | `Telegram` — a host challenges it (`/home/login?authType=Telegram`) |
| Callback | `/signin-telegram` — register `https://<domain>/signin-telegram` with @BotFather for every environment; Telegram does not accept `localhost` |
| Authority | `https://oauth.telegram.org` (discovery, JWKS and token endpoint from its `/.well-known/openid-configuration`) |
| Scopes | `openid profile phone telegram:bot_access` |
| Remote sign-out | off (`/signout-oidc` is not mapped) |
| Back channel | decompresses gzip — Telegram's edge answers its JWKS gzip-encoded unasked about every other time; without it the discovery fails (`IDX20803` / `IDX10805`) |

## The identity

| `ExternalIdentity` | from the `id_token` |
| --- | --- |
| `Subject` | `sub` |
| `Name`, `GivenName`, `FamilyName` | `name`, `given_name`, `family_name` |
| `Picture` | `picture` |
| `Phone`, `PhoneVerified` | `phone_number` (digits, a leading `+` dropped) as a `long`, verified — only when the user shared it |
| `Email` | none: Telegram gives no address |
| `RawClaims[TelegramRawClaims.BotChatId]` | the user's Telegram id (`id`) — the private chat the bot may write to — only when the user let the bot write; absent when they refused |

A verified phone becomes the confirmed phone of a NEW account (unless another account already has it confirmed); it
is never written over an existing account's phone. A host reads the bot chat off `LoginSucceeded.ExternalIdentity`.

## Unverified

Checked against <https://core.telegram.org/bots/telegram-login> and the discovery document on 2026-11-23. Not stated
there, and to be checked on the first real sign-in:

- **How the answer says the bot may write.** The page lists no claim for `telegram:bot_access`. This package reads
  the token response's standard `scope` field; if Telegram omits it, the bot is taken as refused (the safe side: no
  chat id, the user connects notifications by hand). Kept to `TelegramRedirectSignIn.GrantedScopesClaim`.
- **`nonce`.** The page documents one for the JS library only; none is sent (`RequireNonce = false`).
- **`given_name` / `family_name` / `phone_number_verified`** appear in the page's sample token but not in its scope
  table; `PhoneVerified` does not depend on the last one.
- Whether `sub` stays the same for a user across bots; `subject_types_supported` says `public`.
- The key must stay `RS256` (or `ES256`): the page says `EdDSA` and `ES256K` keys reject the `profile` and `phone`
  scopes.

## License

MIT
