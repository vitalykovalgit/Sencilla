# Sencilla.Component.Setting.Mssql

SQL Server schema (`.sql` **source**) for the Sencilla **Setting** component.

Ships the canonical `[dbo].[Setting]` table — a hierarchical, optionally user-scoped
key/value store (`Type` / `ParentId` / `Name` / `Value`).

## How it works

This package ships **schema source**, not a dacpac. When referenced, its `build/*.props`
is auto-imported and compiles the `sql/` source into the **consuming** project's own
model. So `dbo.Setting` lands physically in the consumer's dacpac — **self-contained**:
deploys with no `IncludeCompositeObjects` flag and no co-located dacpacs.

## Consume

```xml
<PackageReference Include="Sencilla.Component.Setting.Mssql" Version="10.0.*" />
```

Then deploy normally: `sqlpackage /Action:Publish /SourceFile:App.dacpac ...`

## Notes

- `[UserId]` will be foreign-keyed to `[sec].[User]` once
  `Sencilla.Component.Users.Mssql` exists (the consuming model that compiles both will
  already contain `sec.User`, so the FK can simply be uncommented).
