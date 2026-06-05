# Sencilla.Authentication.Mssql

SQL Server schema (`.sql` **source**) for the Sencilla **Authentication** component.

Owns `[sec].[RefreshToken]` — the rotating refresh-token ledger used by the JWT issuer.

`RefreshToken.UserId` is a `UNIQUEIDENTIFIER` reference to the owning user with no FK constraint so the package compiles standalone without a Users dependency. The `[sec]` schema is created by `Sencilla.Component.Users.Mssql`; consumers must reference that package in their own database project.

## How it works

This package ships **schema source**, not a dacpac. When referenced, its `build/*.props` is auto-imported and compiles the `sql/` source into the **consuming** project's own model — self-contained, deploys with no extra flags.

## Consume

```xml
<PackageReference Include="Sencilla.Authentication.Mssql" Version="10.0.*" />
```

Pair with the C# EF store: `Sencilla.Authentication.EntityFramework`.
