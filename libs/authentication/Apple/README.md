# Sencilla.Authentication.Apple

Apple (Sign in with Apple) `id_token` verifier for the exchange / native sign-in path. Validates
Apple-signed tokens against Apple's JWKS and the configured audience set.

Apple-specific behavior: the email may be an `@privaterelay.appleid.com` relay (surfaced via the
`is_private_email` raw claim), `email_verified` arrives as a string, and the user's **name is never
in the id_token** (Apple sends it only once at first authorization) — so `Name` is null here and is
captured separately by the exchange endpoint when present.

> The ES256 client-secret-JWT used by the server-side authorization-code flow is **not** part of
> this package (verification needs only Apple's JWKS).

## License

MIT
