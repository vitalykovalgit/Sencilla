# Sencilla.Component.Files.Mssql

SQL Server schema (`.sql` **source**) for the Sencilla **Files** component.

Owns the `[dbo]` file tables:

- `dbo.FileStatus` — lookup table (Pending, Uploaded, Deleted, …)
- `dbo.File` — the core file record (name, MIME type, size, upload progress, storage path, dimensions, resolutions, soft-delete, parent/child hierarchy via `ParentId`)

Self-contained — every foreign key targets another `dbo.*` table in this package (`File → FileStatus`, `File → File` self-ref). `File.UserId` is a `UNIQUEIDENTIFIER` reference to the owning user with no FK constraint so the package compiles standalone without a Users dependency.

## How it works

This package ships **schema source**, not a dacpac. When referenced, its `build/*.props` is auto-imported and compiles the `sql/` source into the **consuming** project's own model. So `dbo.File` and `dbo.FileStatus` land physically in the consumer's dacpac — it is **self-contained**: deploys with no `IncludeCompositeObjects` flag and no co-located dacpacs.

## Consume

```xml
<PackageReference Include="Sencilla.Component.Files.Mssql" Version="10.0.*" />
```

That's it for schema — `dbo.File` and `dbo.FileStatus` are now part of your model. Deploy normally:

```bash
sqlpackage /Action:Publish /SourceFile:App.dacpac ...
```

## Reference data (seed)

`FileStatus` rows ship as **package content** under `seed/` (not compiled into the model). The package auto-defines the SQLCMD variable `$(Sencilla_Component_Files)` pointing at that seed folder. Run it from your post-deployment script:

```sql
:r $(Sencilla_Component_Files)/ApplyData.sql
```

The seed scripts are idempotent and inline at build time, so they run as part of the consumer's normal post-deployment.
