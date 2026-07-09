# Sencilla.Component.Audit

App-wide change-log (who / what / when / why) for opted-in entities.

Mark an entity `IEntityAuditable` and its inserts, updates and deletes are recorded into the `audit.Audit`
table automatically, on the write pipeline, inside the same transaction as the change — via entity-lifecycle
event handlers (no `DbContext` or interceptor changes required).

- **Insert** — recorded from `EntityCreatedEvent<T>` (full-row snapshot).
- **Update** — recorded from `EntityUpdatingEvent<T>` (field-level `{field:{old,new}}` diff against the DB pre-image).
- **Delete** — recorded from `EntityDeletingEvent<T>` (full-row snapshot of the removed rows).

Attribution comes from the ambient `IAuditContext`: actor (`System`/`User`/`Admin`) from the current user,
an optional reason from the `X-Audit-Reason` header (`app.UseSencillaAudit()`), and a per-request
`CorrelationId` that ties a multi-row operation together.

Reference `AddSencillaAudit()` from the host so the assembly loads; discovery wires the rest. Ships the
`audit` schema and `audit.Audit` table via the `Sencilla.Component.Audit.Mssql` source package.
