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
| `IProviderTokenVerifier` | Verifies a provider `id_token` → `ExternalIdentity`. |
| `IAccountLinkingPolicy` | Pure decision: create / link / challenge / reject. |
| `ISecondFactor` | MFA seam (TOTP → passkeys later). |

Failures throw the typed `Sencilla.Core` exceptions; decisions and verifications return values.
Notifications and audit ride Sencilla domain events (`IEventDispatcher`).

## License

MIT
