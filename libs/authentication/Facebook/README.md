# Sencilla.Authentication.Facebook

Facebook **Limited Login** `id_token` verifier for the exchange / native sign-in path. Validates
Facebook-signed OIDC tokens against Facebook's JWKS and the configured audience (app id).

Facebook-specific behavior: the **email may be absent** (the user can decline it) and Facebook does
not assert `email_verified`, so `EmailVerified` is `false` unless the claim is present — the linking
policy then keys purely on the subject. Classic web Facebook Login (access-token + Graph) is the
server-side **redirect** path handled at the IDP host, not this verifier.

> "Instagram" identities ride Facebook Login for Business and are handled through this provider /
> the Facebook Graph, not as a separate IdP.

## License

MIT
