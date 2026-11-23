# Sencilla.Authentication

Abstraction hub for the Sencilla authentication / OIDC identity-provider family. Contains
**only** contracts, records, options, events, and claim constants — no engine, EF, ASP.NET,
or cryptography. Depends on `Sencilla.Core` alone.

Everything else in the family fans into this package:

- `Sencilla.Authentication.Client` — JWKS bearer validation (what resource apps reference).
- `Sencilla.Authentication.Server` — engine-agnostic orchestration + the Argon2id hasher and
  account-linking policy.
- `Sencilla.Authentication.Jwt` — embedded local issuer + refresh reuse-detection.
- `Sencilla.Authentication.OpenIddict` — full OIDC host.
- `Sencilla.Authentication.Users` — default `IUserStore` bridge to `Sencilla.Component.Users`.
- `Sencilla.Authentication.Google` / `.Apple` / `.Facebook` — token-based provider verifiers, and each a redirect
  sign-in.
- `Sencilla.Authentication.Telegram` / `.Instagram` — redirect sign-ins only.

## Key seams

| Type | Purpose |
| ---- | ------- |
| `IUserStore` | User + credential store (find, create-with-credential, link provider, roles). |
| `IPasswordHasher` | Hash / verify / needs-rehash (Argon2id default lives in Server). |
| `IClaimsPrincipalFactory` | Builds the `ClaimsPrincipal` (sub = Guid) running the enricher chain. |
| `IClaimsEnricher` | Optional role/scope enrichment (Security implements it). |
| `ITokenIssuer` | Token materialization for the token-API / embedded path. |
| `ISessionTransport` | How a signed-in principal's session reaches the client: token pair or cookie. |
| `IProviderTokenVerifier` | Verifies a provider `id_token` → `ExternalIdentity`. |
| `IExternalAuthService` | The one resolver from an external identity to its account (`Exchange` an `id_token`, or `ResolveAccount` of an identity the host verified). |
| `IAccountLinkingPolicy` | Pure decision: create / link / link-by-email / reject. A provider-verified address links to the account that owns it (displacing an unverified password credential there); an unverified one never does. |
| `ISecondFactor` | MFA seam (TOTP → passkeys later). |

Failures throw the typed `Sencilla.Core` exceptions; decisions and verifications return values.
Notifications and audit ride Sencilla domain events (`IEventDispatcher`).

## Redirect sign-in

Every provider's redirect sign-in is ASP.NET's own handler for it, bound to the account graph by one piece of this
package (`AuthenticationConfig.AddRedirectSignIn`, the extension point a provider package calls). The binding owns the
two events every remote handler has:

- `OnTicketReceived` — the provider package's reader turns the handler's principal into an `ExternalIdentity`;
  `IExternalAuthService.ResolveAccount` resolves it BEFORE the session exists; the ticket's principal becomes the
  library's own for that account (its id as `sub`), and the session is persistent. A refusal never creates a session.
- `OnRemoteFailure` — whatever failed at the provider (denied, correlation, code exchange) is `<provider>-failed`, as is
  any other failure binding the account.

A sign-in lands on `{SignInFailedUrl}?error=<code>`; without `SignInFailedUrl` the failure is the framework's error.

Every provider is configured the same way under `Authentication:<Provider>` (`RedirectSignInOptions`), plus what it
needs beyond that:

| Key | |
| --- | --- |
| `ClientId` | the client the provider issued |
| `ClientSecret` | the secret the code is redeemed with (Apple signs one from its key instead) |
| `RedirectSignIn` | register the handler (Telegram and Instagram: on by default) |
| `SignInFailedUrl` | where a refused or failed sign-in lands |

`RedirectSignIn` with a required key missing fails at startup, naming it. A host with no keys for a provider in a
deployment does not call its `Use*`.

### Connect

A signed-in user attaches a provider to their account through the same redirect, with the intent written into the
challenge's protected state from the host's session — never from the request:

```csharp
// The host's own challenge, for a signed-in user:
return Challenge(new AuthenticationProperties { RedirectUri = "/profile" }.ConnectToAccountOf(User), "Telegram");
```

The callback reads the intent back from the state, not from the session cookie (Apple posts its answer cross-site). A
connect signs in as that same account and returns to the challenge's `RedirectUri`; refused or failed, it returns there
with `error=<code>` — `provider-linked-to-another-account`, `provider-already-connected` (the account already has a
login of that provider under another subject: one login per provider), `confirm-email-first` (the account's own
address is on it and unconfirmed), `account-not-active`, `<provider>-failed`. When the state cannot be read it lands
on `SignInFailedUrl`.

### Which account an identity becomes

`IAccountLinkingPolicy` decides, the same for a redirect sign-in and for `POST {BasePath}/external`:

| Identity | Decision |
| --- | --- |
| subject already linked | that account (refused `account-not-active` when locked or disabled; refused `provider-linked-to-another-account` on a connect to another account) |
| connect, new subject | linked to the connecting account (`ProviderLinked`) — refused `provider-already-connected` when it already has a login of that provider, `confirm-email-first` while its own address is unconfirmed; an account with no address may connect |
| verified address of an account | linked to it; an unverified password credential there is removed |
| unverified address of an account | refused `email-not-verified` |
| address nobody has, or none | a new account |

An address the provider did not verify (Facebook's, always) is never written to an account: a new account starts
without one, and with app accounts on, our own link goes to it as a pending address
(`IAccountRecoveryService.RequestEmailVerification`), which reaches the account only when followed. A refused address
(`email-invalid`, `email-taken`) does not fail the sign-in. Without app accounts the address is dropped.

`LoginSucceeded.ExternalIdentity` is the identity of every external sign-in — new account, known subject, linked by
address, connect — with the provider's `RawClaims` (e.g. Telegram's bot chat); null for a password sign-in.

### Endpoints

| Route | Session | Answer |
| --- | --- | --- |
| `GET {BasePath}/providers` | no | `200 [{ "provider": "google", "scheme": "Google" }, …]`, in registration order; `[]` when none |
| `GET {BasePath}/logins` | yes | `200 { "providers": ["google", "telegram"], "hasPassword": true }` |
| `DELETE {BasePath}/logins/{provider}` | yes | `200`, also when that provider is not linked; `400 { "error": "last-sign-in-method" }` when it is the only way in (no other provider, no password) |
| `GET {BasePath}/phone-on-another-account` | yes | `200 { "exists": bool }` — the account's phone is confirmed and another live account has the same number |

`/providers` is mapped whenever any sign-in is configured; the other three when a cookie session or a bearer scheme is
too. A connect is the redirect above, not an endpoint.

## Rate limits

The built-in sign-in and recovery endpoints limit themselves (`AuthRateLimiter`, sliding windows
from `System.Threading.RateLimiting`) — no `AddRateLimiter` / `UseRateLimiter` needed. A refused
call answers `429 { "error": "rate-limited" }`.

| Window (`AuthApiOptions.RateLimits`) | Default | Endpoints | Keyed by |
| ---- | ---- | ---- | ---- |
| `LoginPerAddressAndEmail` | 10 / 10 min | `/login` | client address + login |
| `LoginPerAddress` | 50 / 10 min | `/login` | client address |
| `RegisterPerAddress` | 5 / 1 h | `/register` | client address |
| `MailPerRecipient` | 3 / 15 min | `/forgot-password`, `/resend-verification` | email, or the signed-in user's id |
| `MailPerAddress` | 10 / 1 h | `/forgot-password`, `/resend-verification` | client address |
| `TokenPerAddress` | 10 / 1 h | `/reset-password`, `/verify-email`, `/change-password` | client address |
| `ExternalPerAddress` | 10 / 10 min | `/external` | client address |

`/refresh`, `/logout`, `/me`, `/pending-email`, `/providers`, `/logins` and `/phone-on-another-account` are not
limited. The client address is
`HttpContext.Connection.RemoteIpAddress`: behind a proxy, apply the forwarded headers first. Windows
are counted per process, so each replica counts on its own. Change a window with
`c.UseApi(o => o.RateLimits.LoginPerAddress = new(100, TimeSpan.FromMinutes(10)))`; switch them all
off with `c.UseApi(o => o.RateLimits.Enabled = false)`.

## License

MIT
