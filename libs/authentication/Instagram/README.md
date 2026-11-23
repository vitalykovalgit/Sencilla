# Sencilla.Authentication.Instagram

«Business Login for Instagram» for the Sencilla authentication family, as a redirect sign-in on ASP.NET's generic
OAuth handler. The library owns the handler's events; the sign-in resolves through `IExternalAuthService` like every
other provider (see `Sencilla.Authentication`'s README, «Redirect sign-in»). Only professional (business or creator)
Instagram accounts can sign in, and Meta's App Review must pass before anyone outside the app's roles can. Importing
photos is not part of this package.

```csharp
services.AddSencillaAuthentication(configuration, c => c
    .UseCookieSession()
    .UseInstagram(o => o.SignInFailedUrl = "https://example.com/login"));
```

```jsonc
"Authentication": {
  "Instagram": {
    "ClientId": "<Instagram App ID>",        // App Dashboard → Instagram → API setup with Instagram login
    "ClientSecret": "<Instagram App Secret>",
    "SignInFailedUrl": "https://example.com/login"
  }
}
```

`RedirectSignIn` starts on (it is Instagram's only shape), so `UseInstagram` without a `ClientId` and a `ClientSecret`
fails at startup — a host with no keys in a deployment does not call it.

## What it registers

| | |
| --- | --- |
| Scheme | `Instagram` — a host challenges it (`/home/login?authType=Instagram`) |
| Callback | `/signin-instagram` — register `https://<domain>/signin-instagram` as an OAuth redirect URI in the app's Business Login settings |
| Authorization | `https://www.instagram.com/oauth/authorize`, scope `instagram_business_basic` |
| Code exchange | `POST https://api.instagram.com/oauth/access_token` |
| Profile | `GET https://graph.instagram.com/me?fields=user_id,username,profile_picture_url` |

## The identity

| `ExternalIdentity` | from `/me` |
| --- | --- |
| `Subject` | `user_id` |
| `Name` | `username` (never split into a first and a last name) |
| `Picture` | `profile_picture_url` |
| `Email`, `Phone` | none: Instagram gives neither, so the account starts with no contact |

## Unverified

Checked against Meta's [Business Login](https://developers.facebook.com/docs/instagram-platform/instagram-api-with-instagram-login/business-login)
and [Get Started](https://developers.facebook.com/documentation/instagram-platform/instagram-api-with-instagram-login/get-started)
pages on 2026-11-23. Not stated there, or stated ambiguously, and to be checked on the first real sign-in:

- **The answers' shape.** Both pages' samples wrap the result in `{ "data": [ { … } ] }`; the handler takes the first
  entry, or a flat object as it is (`InstagramRedirectSignIn.FirstEntryOf`).
- **The code exchange's encoding.** Meta's sample posts multipart form fields (`curl -F`); the handler posts
  `application/x-www-form-urlencoded`, as every OAuth handler does.
- **Which id is the subject.** `/me` has `id` (app-scoped) and `user_id` (the professional account's id); the code
  exchange answers a `user_id` too. `user_id` from `/me` is used; whether it equals the exchange's is not stated.
- **The Graph version.** The Get Started sample calls `v27.0`; the unversioned host uses the app's default version.

## License

MIT
