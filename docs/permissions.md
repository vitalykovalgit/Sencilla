# Session goal

Make code review and critiques permissions managment in sencilla (packages, Sencilla.Component.Users, Sencilla.Component.Security)

then grill me on next tasks, but first I will describe you my permission architecture how I see it and how I want it to work, then we will grill on improvements and implementations

## Overview

The Sencilla.Component.Users and Sencilla.Component.Security is aim for protect the entity api for sencilla entities (See Core package like IEntity, IEntityUpdatable, IEntityCreatable... )

The main idea is that every API/repository (basically any! entity operation) operation must go through permissions and must be approved (if not it must be denied by default)

## Permission matrix

The permission matrix is the heart of permission system, it store all the rules who can do what with entity and which limitation entity has

we have next base operations, basically CRUD

Operations:
- CREATE
- READ
- UPDATE
- DELETE

We have an entity as a string, 

We have roles, we have 4 base roles:
1 - root ( can do all in app, built in role)
2 - admin ( admin role)
3 - user ( logged in user )
4 - anonymous (no user)

I'm thinking to add also 'guest' but not sure

the list of roles is dynamic, admin can add roles on the fly as well (on admin page), or it can be added by the deployment script, so roles is fully dynamic

So permission matrix looks like this:

-------------------------------------------
| Role  | Entity | Operation | Constraints |
-------------------------------------------
| root |  '*'/'all' |  '*'/'all' | NULL |

| Admin | Order | Read   | NULL |
| Admin | Order | Create | NULL |
| Admin | Order | DELETE | NULL |

| User | Order | Read | 'UserId == CurrentUser.Id' |
...
| Anonymous | Sizes  | Read | NULL |
| Anonymous | Prices | Read | NULL |

Roles are cumulative (I'm still thinking how to better do it) 
So 'anonymous' permissions has all users who come to application/site
So 'user' permissions has all users who logged in to application/site + 'anonymous'
So 'admin' has all 'user' + 'anonymous' permissins

Let's think how to make role inheritance, for example if I want to create a role 'manager' or 'photograf' which is based on 'user' role

## How it is implemented in C#

3 source of permissions
- DB matrix
- Attributes (like CanRead(Admin, User), Permission(Admin, Read, "some constraints")
- Builder (like specification pattern https://github.com/ardalis/Specification )
- 

## Constraints

Constrains is build on top of dynamic linq library (https://dynamic-linq.net/) so we can write the constrains to the query in the permissions matrix dynamically without compiling the code 

for example if I have the below table 

order
----------
Id, UserId, Name, Desc, State, Kind, CreatedDate... 

I can create a rule in matrix like

Role_User | order | Read | UserId = {User}.Id 

where {User}.Id is read from the IEnvironmentVariables class 

It is build for read operation throught the reposiroty constraints pattern (based on this pattern filtering is build) 

I need to extend this approach to the create and update and delete operations, so before touching DB I can validate if object does not break the constarins on C# side for the entities that comes from the client side. 

I also really like the specification pattern, where I can add specification (like permission specification) and use it for the entity validation and query validation

Let's think how we can get bets out of this two worlds/approaches or integrate both so developer can select the best one for its use cases

## Role/User resourses assignments

Sometimes we need to assign some resources to the user and assign specific roles to that resources (what described above more system level) in this case I like below approach

Let's say I have the next entity 'Project' then I create ProjectUser entity, we can create <Entity>User table for eny resource that we want to protect or have role based access to it.
 The table format is next 
 
| Id | UserId | EntityId | Role |

So in permission matrix I can write constraints like this

|Role_User | 'Project' | Read | Project.Users.Has({User}.Id)

where Project.User is navigation property

Let's think how we can implement this approach for the crete/update/delete operations

Let's also discuss other similar approaches

Also I want to discuss with you how we must create an entity in generic way with user in separate table, for example when user create an project I want to create this two records in the table 

Project
----------
Id, Name... 
1,  'My Photos' ... 

ProjectUser
-------------
Id, User, Entity, Role 
1,  2,    3,     'owner' 

## Design decisions (grill-me interview, 2026-07-06)

Every operation below was decided branch-by-branch; the order within "Implementation roadmap" is dependency order.

### Enforcement model

1. **Pre-image in SQL for UPDATE/DELETE.** The constraint is composed into the query against current DB state (never evaluated against the client-supplied object — that allows hijacking rows by forging `UserId`). Nav-prop rules like `Project.Users.Any(...)` evaluate in SQL. Pre-image fetches are `AsNoTracking` (change-tracker collision with `UpdateRange`).
2. **UPDATE checks pre + post image.** Post-image = save inside the ambient transaction, re-run the constraint as SQL (`COUNT WHERE Id IN (...) AND constraint == N`), rollback + 403 on violation. Prevents giving rows away (reassigning `UserId`). In-memory post-checks are wrong for nav-prop rules — always requery.
3. **Action becomes a flags enum**: `Read=1, Create=2, Update=4, Delete=8, All=15` (the commented-out version in `Action.cs`). Requires a one-time migration of `sec.Matrix` rows (3→4, 4→8, 5→15). Fixes the current bug where `All=5` fails Create/Update checks and `Update=3` accidentally matches Read/Create.
4. **Bulk denial is all-or-nothing**: any denied entity in a batch → `ForbiddenException`, nothing persisted. Nonexistent ids do not count as denied (no existence leak on delete).
5. **Write ops get an automatic ambient transaction** spanning `-ing` event → save → post-check → `-ed` handlers → commit (reuses the caller's transaction if one is open). Accepted limit: `-ed` handlers run pre-commit, so external side effects can fire for rolled-back writes (post-commit event = parked branch).
6. **Operation → Action mapping**: Remove/Undo → **Delete** (by intent, though implemented as update); Hide/Show → **Update**; Upsert/Merge/GetOrCreate → **per-row by existence** (inserting rows → Create check, updating rows → Update check, Merge removals → Delete check); JsonMerge → **Update**.

### Enforcement boundary

7. **JsonMergeAsync is routed through the Update pipeline** (constraint-composed WHERE on the id + post-check requery). Today it bypasses events entirely.
8. **New constrained `QueryAsync()`** publishes `EntityReadingEvent`; raw `Query`/`Where` become an explicit unsafe escape hatch (restricted/obsoleted for app code), used by framework internals only. Today raw `Query` is an unconstrained read bypass.
9. **EF provider only for now.** Secured entities require the EF repositories; HttpClient/SqlMapper enforcement is out of scope (parked: decorator-based event publication).
10. **Framework internals use the raw non-evented path** (pre-image fetch, post-check requery, `UserRole` lookup) — no recursion by construction. `Access.Root()` is demoted to an explicit app-level "run as system" escape only.

### Matrix & roles

11. **Rows OR everywhere — grants accumulate.** A rule needing two conditions writes `&&` inside one constraint. (Change from today's AND-within-role, which made "add row" narrow access.)
12. **Role inheritance = `ParentId` on `sec.Role`**, transitive closure expanded in `ResolveRoleIds`, cached; cycle detection at role save (the graph is admin-editable). Base cumulativity (everyone ⊇ anonymous, authenticated ⊇ user) stays as-is via authentication in `ResolveRoleIds` — inheritance is only for chains beyond that (photographer ⊇ manager).
13. **Root = hard-coded bypass in code** before any matrix lookup. Immune to matrix misconfig, cache staleness, and admin lockout. Wildcard `'*'` resource matching is therefore optional, not load-bearing.
14. **Cache invalidation is event-driven**: `EntityCreated/Updated/DeletedEvent` handlers on `Matrix`/`Role`/`UserRole` evict the permission and role caches, with a short TTL backstop (5–15 min, down from 180). Multi-node invalidation = parked (TTL covers it).

### Create & ownership

15. **New `IEntityCreatedByTrack` / `IEntityUpdatedByTrack`** (nullable Guid), stamped from the current user in the write pipeline, never client-writable. Gives every opted-in entity a constraint target (`CreatedBy == {user.id}`) plus audit trail.
16. **Create constraints validate the incoming object; violation → 403.** No auto-fixing of business fields (`Order.UserId`) — silent mutation hides client bugs. Stamping is only for framework audit fields.
17. **Owner-row provisioning**: `[OwnedBy(typeof(ProjectUser))]` on the entity + an open-generic `EntityCreatedEvent` handler creates the link row (`UserId`=current, `EntityId`=new id, Role=owner) inside the ambient transaction — atomic with the entity by decision 5. Note: `Project.Users.Has(...)` rules are unsatisfiable *at* create time; create rules can only check incoming fields, DB-state rules start working from the next operation.
18. **`<Entity>User.Role` is an FK to `sec.Role`.**
19. **`sec.Role` gets a `Kind` column (`System` | `Resource`) with hard validation**: `ResolveRoleIds`/inheritance expansion filter to System; saving a `UserRole` or `Matrix` row referencing a Resource role, or cross-kind parenting, is rejected at write time. Without this, assigning `owner` as a global role silently escalates.

### Permission sources

20. **Attribute/builder rules get lazy name→`RoleId` resolution** against `sec.Role`, cached with the same eviction; unknown role names log a warning. (Today those sources are inert — `RoleId` never populated, attribute path commented out of registration.)
21. **Common rule currency = expression trees** under an `IPermissionRule<TEntity>` exposing `Expression<Func<T,bool>>`. DB matrix strings parse via dynamic LINQ (existing); code-side specifications supply expressions directly; both feed the same enforcement points (queryable for read/update/delete pre+post, compiled for create). Dead `Expressions/` scaffolding gets deleted.
22. **Merge policy across sources = union.** Code-shipped grants are developer-owned product policy; revoking one is a code change. Accepted limit: no runtime kill-switch for shipped grants until a DENY effect exists.

### Parked branches (deliberately deferred)

- DENY / `Effect` column (runtime revocation, exception rules like "all except archived")
- Field-level permissions — blocked on partial updates (`Update` is a full-row overwrite today)
- Post-commit event for external side effects (emails, webhooks)
- Multi-node distributed cache invalidation
- Non-EF provider enforcement (decorator around `IXxxRepository`; HttpClient is remote-enforced)
- Resource-role hierarchy (owner ⊇ editor ⊇ viewer) expanded at rule-compile time into `RoleId IN (...)`
- Wildcard `'*'` resource matching

### Implementation roadmap (dependency order)

1. ~~Action flags migration + fix `SecurityProvider` matching (blocks all wiring).~~ **Done 2026-07-06**: `[Flags]` Action (Read=1, Create=2, Update=4, Delete=8, All=15); `Data/MigrateActionFlags.sql` (one-time guarded migration, included first in `ApplyData.sql`); `ActionData.sql` seeds all 15 flag combinations (FK_Matrix_Action allows combined grants); regression tests in `SecurityProviderActionMatchingTests`.
2. ~~Ambient transaction infrastructure in write repositories.~~ **Done 2026-07-06**: `BaseRepository.InTransaction` wraps Create/Upsert/Merge/GetOrCreate/Update/Delete (-ing event → save → -ed handlers → commit); reuses the caller's open transaction; skipped on non-relational providers (InMemory). Raw `DbCommand`s in the GetOrCreate bulk extensions now enlist in the ambient transaction. Sqlite-based rollback tests in `AmbientTransactionTests`.
3. ~~UPDATE enforcement: register the update handler, pre-image + post-check requery. DELETE: add events, constraint-composed `ExecuteDelete`, denied-vs-nonexistent distinction. Remove/Undo mapped to Delete action.~~ **Done 2026-07-06**: `EntityUpdatingEvent`/`EntityUpdatedEvent` carry `DbEntities` (pre-/post-image DB query); handlers compose constraints into it, repositories apply all-or-nothing denial via `ThrowIfNarrowed` (nonexistent rows are not denied). Security handler registered for updating/updated/deleting; post-image violations roll back via the ambient transaction. Delete fires deleting/deleted events and executes through the narrowed query (constraint inside the DELETE's WHERE). Remove/Undo publish delete events (Delete action), persisting through `PersistUpdate` without update events. Tests: `WritePathEnforcementTests` (choreography, Sqlite), `SecurityConstraintWritePathTests` (handler composition).
4. ~~Per-row Upsert/Merge checks; JsonMerge through the pipeline; `QueryAsync` + raw-`Query` restriction.~~ **Done 2026-07-06**: Upsert/Merge probe existence by match key and split checks per row (new→Create in-memory, existing→Update pre+post against a query scoped to pre-existing keys, Merge removals→Delete, each skipped when its group is empty); GetOrCreate checks Create only for rows that will insert (all rows when a match filter is present — over-strict, never under-enforced). JsonMerge (Update+AppendOnly repos) runs the Update pre/post pipeline. `IReadRepository.QueryAsync` applies the reading pipeline (default interface impl falls back to raw for non-EF providers); `Query`/`Where` documented as UNSAFE escape hatches. Tests: `PerRowEnforcementTests`.
5. ~~OR row-combination + root bypass + event-driven cache eviction.~~ **Done 2026-07-06**: `ApplyConstraint` now ORs all applicable rows (any unconstrained row = full grant — adding a row can only widen access); `RoleType.Root` short-circuits before any matrix lookup; `SecurityCacheSignal` (singleton change-token) evicted by `SecurityCacheEvictionHandler` on Matrix/Role/UserRole created/updated/deleted events (signal-wide — payload-free deletes can't target entries), permission cache TTL 180→15 min backstop, per-user role cache subscribes to the same signal. Tests: row-combination/root cases in `SecurityConstraintWritePathTests`, `SecurityCacheEvictionTests`.
6. ~~`CreatedBy`/`UpdatedBy` tracks, `[OwnedBy]` provisioning, `Role.Kind` + validation.~~ **Done 2026-07-06**: `IEntityCreatedByTrack`/`IEntityUpdatedByTrack` (nullable Guid) stamped by `TrackStampHandler` (Users component, auto-registered per entity, always overwrites client values); `[OwnedBy(typeof(ProjectUser))]` + `IOwnershipLink<TKey>` convention + `OwnershipProvisioningHandler` creates the owner link row on the created event inside the ambient transaction under `Access.Root()` (seeded 'Owner' role id 6, Resource kind); `Role.Kind` column + `RoleKind` enum + `RoleValidationHandler` rejects Resource roles in UserRole/Matrix and cross-kind/cyclic/missing parents.
7. ~~`IPermissionRule<T>` refactor + attribute/builder RoleId fix (re-enable attribute registration).~~ **Done 2026-07-06**: `IPermissionRule<TEntity>` (RoleId + Action flags + `Expression<Func<T,bool>>? Constraint(sysVars)`) resolved from DI and unioned with matrix rows in `ApplyConstraint` (null constraint = full grant); attribute/builder rows get lazy name→RoleId resolution in `SecurityProvider` (cached with the same eviction; unknown names logged + dropped) — attribute discovery was already wired via `SecurityAttributeDiscoverer`, only the RoleId gap blocked it; dead `Expressions/` scaffolding deleted.
8. ~~Role inheritance (`ParentId` + cached closure + cycle detection).~~ **Done 2026-07-06**: `Role.Parent` column (self-FK); `IRoleClosure`/`RoleClosure` expands ancestor chains from a cached snapshot (signal-evicted, 15-min TTL, drops Resource-kind roles, tolerates pre-existing cycles), applied in `ApplyConstraint` after the root bypass; cycle/cross-kind/missing-parent writes rejected by `RoleValidationHandler`. `RoleData.sql` seeds Kind/Parent + Owner and now only deletes seeded-range rows (Id < 100) — custom roles survive redeploys.

Each step needs tests — today the only security test covers `ResolveRoleIds`; nothing exercises constraint application or the 403 path end-to-end.


