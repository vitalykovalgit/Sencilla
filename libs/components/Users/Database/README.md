# Sencilla.Component.Users.Mssql

SQL Server schema (`.sql` **source**) for the Sencilla **Users** component.

Owns the `[sec]` schema and the user-identity tables:

- `sec.User` — the core identity (email/phone, name, status/type/gender, audit dates)
- `sec.UserAuth` — credentials / external logins (`PasswordHash` nullable)
- `sec.UserClaim`, `sec.UserContact`, `sec.UserAddress`
- reference tables: `sec.UserType`, `sec.UserStatus`, `sec.UserGender`,
  `sec.UserContactType`, `sec.UserAddressType`

Self-contained — every foreign key targets another `sec.*` table. This is the **keystone**
schema component: it creates `[sec]`, so [[Sencilla.Component.Security.Mssql]] and any
database that needs `sec.User` compile this package into their own model.

## How it works

This package ships **schema source**, not a dacpac. When referenced, its `build/*.props`
is auto-imported and compiles the `sql/` source into the **consuming** project's own
model. So `sec.*` lands physically in the consumer's dacpac — it is **self-contained**:
deploys with no `IncludeCompositeObjects` flag and no co-located dacpacs.

## Consume

```xml
<PackageReference Include="Sencilla.Component.Users.Mssql" Version="10.0.*" />
```

That's it for schema — `sec.*` is now part of your model. Deploy normally:

```bash
sqlpackage /Action:Publish /SourceFile:App.dacpac ...
```

## Reference data (seed)

Lookup rows (user types / statuses / genders / contact + address types) ship as **package
content** under `seed/` (not compiled into the model). The package auto-defines a SQLCMD
variable (via `build/*.props`) named after the package — PackageId with `.Mssql` dropped and
dots replaced by underscores — `$(Sencilla_Component_Users)`, pointing at that seed folder.
It also provides an ordered entrypoint `ApplyData.sql`. Run it from your post-deployment
script with one line:

```sql
:r $(Sencilla_Component_Users)/ApplyData.sql
```

The seed scripts are idempotent (`IF NOT EXISTS ... SET IDENTITY_INSERT`) and inline at
build time, so they run as part of the consumer's normal post-deployment.

## Related

- [[Sencilla.Component.Geography.Mssql]] — `geo.*` (referenced by `sec.UserAddress` columns, no FK)
- [[Sencilla.Component.Security.Mssql]] — `sec.Role` / `sec.UserRole` / ... (depends on this)
- [[Sencilla.Component.Setting.Mssql]] — `dbo.Setting` (will FK to `sec.User`)
