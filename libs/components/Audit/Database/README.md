# Sencilla.Component.Audit.Mssql

SQL Server schema (`.sql` source) for the Sencilla Audit component: the `[audit]` schema, the app-wide
`audit.Audit` change-log table, and the `AuditAction` / `ActorType` enum-lookup tables (FK-referenced by
`audit.Audit.[Action]` / `[ActorType]` for enum-value integrity).

Self-contained (every FK targets another `audit.*` table) and shipped as **source** — a consuming
`Microsoft.Build.Sql` project that references this package compiles `audit.*` physically into its own model
via the auto-imported `build/Sencilla.Component.Audit.Mssql.props`.

Reference data (the lookup rows) ships as **seed** under `seed/`, not compiled into the model. Consumers run
it with one line in their own post-deployment script:

```sql
:r $(Sencilla_Component_Audit)/ApplyData.sql
```

`$(Sencilla_Component_Audit)` is defined automatically by the package's `build/*.props`.
