# Sencilla.Component.Security.Mssql

SQL Server schema (`.sql` **source**) for the Sencilla **Security** component.

Owns the RBAC objects in the `[sec]` schema:

- `sec.Role` — named roles (identity starts at 100)
- `sec.Action` — named actions (Read, Write, Delete, …)
- `sec.Area` — named areas / resource groups
- `sec.Matrix` — permission rules linking a role, resource, and action (with optional constraint expression)
- `sec.UserRole` — assignment of roles to users (FK to `sec.User`)

Depends on [[Sencilla.Component.Users.Mssql]] — `sec.UserRole.UserId` references `sec.User.Id`, so the Users package must be compiled into the same model.

## How it works

This package ships **schema source**, not a dacpac. When referenced, its `build/*.props` is auto-imported and compiles the `sql/` source into the **consuming** project's own model. So all `sec.*` RBAC objects land physically in the consumer's dacpac — self-contained: deploys with no `IncludeCompositeObjects` flag and no co-located dacpacs.

## Consume

```xml
<PackageReference Include="Sencilla.Component.Users.Mssql"   Version="10.0.*" />
<PackageReference Include="Sencilla.Component.Security.Mssql" Version="10.0.*" />
```

Deploy normally:

```bash
sqlpackage /Action:Publish /SourceFile:App.dacpac ...
```

## Reference data (seed)

Role, Action, and Area seed rows ship as **package content** under `seed/` (not compiled into the model). The package auto-defines the SQLCMD variable `$(Sencilla_Component_Security)` pointing at that seed folder. Run it from your post-deployment script:

```sql
:r $(Sencilla_Component_Security)/ApplyData.sql
```

The seed scripts are idempotent and inline at build time, so they run as part of the consumer's normal post-deployment.
