# Sencilla.Component.Geography.Mssql

SQL Server schema (`.sql` **source**) for the Sencilla **Geography** component.

Owns the `[geo]` schema and the full administrative hierarchy:

- `geo.Country`, `geo.Language`, `geo.CountryLanguage`
- `geo.State`, `geo.Region`, `geo.City`, `geo.District`

Self-contained — every foreign key targets another `geo.*` table.

## How it works

This package ships **schema source**, not a dacpac. When referenced, its `build/*.props`
is auto-imported and compiles the `sql/` source into the **consuming** project's own
model. So `geo.*` lands physically in the consumer's dacpac — it is **self-contained**:
deploys with no `IncludeCompositeObjects` flag and no co-located dacpacs.

## Consume

```xml
<PackageReference Include="Sencilla.Component.Geography.Mssql" Version="10.0.*" />
```

That's it for schema — `geo.*` is now part of your model. Deploy normally:

```bash
sqlpackage /Action:Publish /SourceFile:App.dacpac ...
```

## Reference data (seed)

Countries / languages ship as **package content** under `seed/` (not compiled into the
model). The package auto-defines a SQLCMD variable (via `build/*.props`) named after the
package — PackageId with `.Mssql` dropped and dots replaced by underscores —
`$(Sencilla_Component_Geography)`, pointing at that seed folder. It also provides an
ordered entrypoint `ApplyData.sql`. Run it from your post-deployment script with one line:

```sql
:r $(Sencilla_Component_Geography)/ApplyData.sql
```

The seed scripts are idempotent (`IF NOT EXISTS ... SET IDENTITY_INSERT`) and inline at
build time, so they run as part of the consumer's normal post-deployment.
