# Sencilla.Component.Tags.Mssql

SQL Server schema source for the Sencilla **Tags** component: the `[tag]` schema and the shared
`tag.EntityTag` table.

Ships **source**, not a dacpac. A consuming `Microsoft.Build.Sql` project that references this package
compiles `sql/*.sql` into its own model (via the auto-imported `build/Sencilla.Component.Tags.Mssql.props`),
so `tag.*` lands physically in the consumer's dacpac — self-contained, no composite reference.

## What needs this package

Only [[IEntityTaggableShared]] storage. The other two strategies ship no schema here:

| Strategy | Where the tags live | DDL owner |
| --- | --- | --- |
| `IEntityTaggableInline` | `[Tags] NVARCHAR(4000) NULL` on the entity's own table | the adopter |
| `IEntityTaggableLinked` | `{Entity}Tag(Id, EntityId, Name, CreatedDate)` with a cascading FK | the adopter |
| `IEntityTaggableShared` | `tag.EntityTag(Entity, EntityId, Name)` | **this package** |

Referencing the package while using only inline or linked storage leaves an empty `tag.EntityTag` behind.
That is intentional: an empty table costs nothing, and it makes adopting shared storage later a code change
rather than a database-package change.

## Notes

- `EntityId` is `NVARCHAR(64)` with **no** foreign key — that is the trade the shared repository makes (and the
  same one `audit.Audit` makes). Orphans left by a hard delete are swept by the component's
  `EntityDeletingEvent` handler; soft-deleted rows keep their tags.
- `Name` is normalised by `Sencilla.Core`'s `TagName` (lowercase, trimmed, `[a-z0-9-_.:]`). The default CI
  model collation makes `UX_EntityTag_Row` case-insensitive, consistent with that normalisation.
- No seed data — the tag vocabulary is free text.
