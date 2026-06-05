# Sencilla.Authentication.Google

Google `id_token` verifier (`IProviderTokenVerifier`) for the exchange / native sign-in path.
Validates Google-signed tokens against Google's JWKS and the configured audience set, then maps
them to a normalized `ExternalIdentity`.

```jsonc
"Authentication": {
  "Google": { "Audiences": [ "<web-client-id>", "<ios-client-id>" ] }
}
```

Bind with `services.Configure<GoogleProviderOptions>(config.GetSection("Authentication:Google"))`.

## License

MIT
