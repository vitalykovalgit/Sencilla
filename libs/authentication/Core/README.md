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
- `Sencilla.Authentication.Google` / `.Apple` / `.Facebook` — token-based provider verifiers.

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

`/refresh`, `/logout` and `/me` are not limited. The client address is
`HttpContext.Connection.RemoteIpAddress`: behind a proxy, apply the forwarded headers first. Windows
are counted per process, so each replica counts on its own. Change a window with
`c.UseApi(o => o.RateLimits.LoginPerAddress = new(100, TimeSpan.FromMinutes(10)))`; switch them all
off with `c.UseApi(o => o.RateLimits.Enabled = false)`.

## License

MIT
