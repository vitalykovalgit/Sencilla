# Sencilla.Component.Tags

Free-text tagging for **any** Sencilla entity: implement one interface, get `?tag=` filtering, tag endpoints
and a `tags: string[]` field on the wire.

```csharp
[CrudApi("api/v1/pricerules")]
public class PriceRule : IEntity<Guid>, IEntityTaggableInline
{
    public Guid Id { get; set; }
    public List<string>? Tags { get; set; }   // NVARCHAR(4000) NULL on the table
}
```

```
GET    /api/v1/pricerules?tag=promo&tag=delivery   # rows carrying ANY of these tags
GET    /api/v1/pricerules?with=tags                # ...and fill tags[] on them (side-table storage only)
GET    /api/v1/pricerules/tags                     # every tag in use — autocomplete
POST   /api/v1/pricerules/{id}/tags                # ["promo","delivery"] — REPLACES the set
DELETE /api/v1/pricerules/{id}/tags?tag=promo      # remove named tags
```

## Three repositories, one contract

`IEntityTaggable.Tags` is the wire contract for all three; consumers never learn where the rows live, so an
entity can migrate between repositories without a single consumer changing.

| Interface | Storage | Pick it when | DDL |
| --- | --- | --- | --- |
| `IEntityTaggableInline` | JSON array column on the entity's own row | the table is small or loaded wholesale (tags arrive with the row; no join, no second query) | `[Tags] NVARCHAR(4000) NULL` on your table |
| `IEntityTaggableLinked` | `{Entity}Tag` — typed FK, `ON DELETE CASCADE` | large/hot tables that must filter by tag in SQL | your own table + `class XTag : EntityTagLink<X, TKey> {}` |
| `IEntityTaggableShared` | `tag.EntityTag(Entity, EntityId, Name)` | tagging with **no** DDL of your own, or cross-entity tag questions | reference `Sencilla.Component.Tags.Mssql` |

Exactly one storage interface per entity — `TagRegistrator` fails at startup otherwise, as it does for a
linked entity missing its (necessarily **non-generic**) link entity.

## Layout

```text
Contract/   IEntityTaggable + the three storage markers, ITagRepository
Entity/     TagName (what a valid tag IS) — EntityTagBase → EntityTagLink<TEntity,TKey> | EntityTag
Impl/Repo/  TagReadRepository (abstract) → TagInlineRepository
                                         → TagLinkedRepository → TagSharedRepository
Impl/Handlers/  ?tag= filtering, tags[] hydration, orphan sweeping
Impl/       TagRegistrator (wiring), TagsModelConfigurator (EF mapping), TagKey (key ⇄ text)
Web/        TagApiController — the four endpoints above
Database/   the [tag] schema package
```

The endpoints ship here rather than in `CrudApiController`, and mount themselves onto every `[CrudApi]`
entity's route through `Sencilla.Web`'s `[EntityApi]` seam — an open generic `Controller<TEntity, TKey>` marked
with it is closed over each entity and routed under that entity's route. A host that doesn't reference this
package therefore has no tag routes at all, rather than routes that answer 501.

The repositories are `ReadRepository<TEntity, DynamicDbContext, TKey>` subclasses, so the DbContext, the
resolver, `Save` and the ambient-transaction helper come from the framework rather than being rebuilt.
`TagSharedRepository` **is** a `TagLinkedRepository` whose foreign key was traded for a type discriminator plus
a stringified id: it inherits every row-shaped operation and overrides only the four addressing seams. The link
table's key is read and written through EF's model *by name* (`EF.Property`), which is what lets one
implementation serve any adopter's link entity.

## Guarantees

- **Normalised** by `TagName`: lowercase, trimmed, deduped, ordinally sorted, charset
  `a-z 0-9 - _ . :` (`:` is for namespacing, e.g. `promo:black-friday`). Invalid input is rejected with an
  error code, never silently rewritten. This is what keeps ordinal comparison in a consumer and SQL Server's
  case-insensitive collation from ever disagreeing.
- **A tag write is a write of the tagged row** — `UpdatedDate` moves and the audit log records it, so caches
  keyed on the row invalidate. Side-table writes are diffed (an unchanged set writes nothing) and atomic.
- **Authorisation comes from the parent.** The endpoints load the tagged row through its own read repository
  first, so tag permissions are the row's permissions — tags are never a back door around them.
- **`?tag=` composes** with permission constraints and every other filter (it is a reading-pipeline handler),
  and `?tag=` with only malformed values matches nothing rather than widening to everything.
- **Reading tags back is opt-in**, on `?with=tags`, so a list read pays the side table's second query only when
  the caller wants tags. It cannot be a real EF `Include`: `Tags` is a primitive collection for inline storage,
  ignored by the model for the other two, and the shared table has no foreign key to navigate — so
  `FilterConstraintHandler` drops `with=tags` as a non-navigation and the hydration handler gives it meaning.
  Inline entities ignore the flag; their column always arrives with the row. Single-row `GET {route}/{id}`
  cannot carry `?with=` at all (`IReadRepository.GetById` takes no filter) — use `GET {route}/{id}/tags`.

## Wiring

`AddSencilla()`'s assembly scan registers everything. `AddSencillaTags()` exists for hosts that skip the scan
and is idempotent.

Storage notes: `NULL` means "no tags" (never `[]`); consumers treat null, absent and non-array alike as empty.
Soft-deleted rows keep their tags; hard deletes cascade (linked) or are swept (shared).
