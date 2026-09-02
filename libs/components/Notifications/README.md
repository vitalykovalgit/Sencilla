# Sencilla.Component.Notifications

In-app notifications. A notification is a **class**, not a lookup row: declare it, write it,
render it — no enum, no seed, no schema change per kind.

```csharp
[NotificationType("project.cloned")]          // optional; defaults to the class name
public class ProjectClonedNotification
{
    public Guid ProjectId { get; set; }
    public string? ProjectName { get; set; }
}
```

Four objects:

| | |
| --- | --- |
| `Notification` | the event: `Type` (wire key), `Data` (json), `CreatedDate`. **No `UserId`.** |
| `NotificationTarget` | who receives it. Rows UNION; the two target slots within a row AND. |
| `NotificationRead` | per-user read state. A null `NotificationId` is a **watermark**. |
| `NotificationInbox` | the read surface, mapped to `NotificationInboxView` — one row per recipient, with that user's read stamp resolved. |

Only `NotificationRead` (create/read) and `NotificationInbox` (read) are client-facing. The other
two are written by trusted app code.

Packages: `Sencilla.Component.Notifications` (this library) and
`Sencilla.Component.Notifications.Mssql` (schema source, compiled into the consuming dacpac;
requires `Sencilla.Component.Users.Mssql` and `Sencilla.Component.Security.Mssql` for the
`sec.User` / `sec.Role` / `sec.UserRole` objects).

## Adopting

1. Reference both packages; register endpoints in the web host:
   `services.AddSencillaEndpoints(typeof(Notification).Assembly)`.
2. Seed matrix grants — and **only** these:

   ```sql
   (@Role_User, N'notificationinboxview', @ActionRead,   N'userId={user}.Id'),
   (@Role_User, N'notificationread',      @ActionCreate, N'userId={user}.Id'),
   (@Role_User, N'notificationread',      @ActionRead,   N'userId={user}.Id'),
   ```

   No Update or Delete grant anywhere. A CrudApi update is a full-row replace, so an Update grant
   on either table lets a user rewrite the notification they were sent or forge their own read
   watermark.
3. Write from trusted code — one `Notification` plus one `NotificationTarget` per audience,
   in the caller's transaction:

   ```csharp
   var notification = new Notification
   {
       Type = NotificationType.KeyOf<ProjectClonedNotification>(),
       Data = NotificationType.Serialize(new ProjectClonedNotification { ProjectId = id }),
       CreatedDate = DateTime.UtcNow,
   };
   await notifications.Create(notification, token);
   await targets.Create(new NotificationTarget
   {
       NotificationId = notification.Id,
       Scope = (byte)NotificationScope.User,
       TargetGuid = recipientId,
   }, token);
   ```

4. Run a retention sweep. One delete on `Notification` cascades to both children; superseded
   watermarks need a second statement — see [Retention](#retention).

## Scopes

`User` (1), `Role` (2) and `System` (3) are resolved by `NotificationInboxView`. **`System` stays
one row** however many users exist — the view cross-joins, so a broadcast is not a fan-out.

Values from `NotificationScope.AppScope` (100) up are the consuming app's own. The component does
not resolve them: an app either expands its scope into `User` target rows when it writes (cheapest,
and the audience is snapshotted at send time), or replaces the view with one that adds its own
branch.

Role matching is **exact** and does not expand `sec.Role.Parent`, whereas the permission matrix
does. "Roles for audience" and "roles for permissions" are deliberately not the same set, so
targeting Editor does not reach Owner.

## Retention

```csharp
// Notifications past the retention window. Cascades take their targets and read rows.
await notifications.Query.Where(n => n.CreatedDate < cutoff).ExecuteDeleteAsync(token);

// Superseded watermarks: keep only each user's newest.
await reads.Query
    .Where(r => r.NotificationId == null
             && reads.Query.Any(o => o.NotificationId == null
                                  && o.UserId == r.UserId
                                  && o.CreatedDate > r.CreatedDate))
    .ExecuteDeleteAsync(token);
```

`ExecuteDeleteAsync` bypasses the change tracker and never consults EF's `DeleteBehavior`, so the
cascades must be — and are — in the database. Watermark rows survive the first delete for free:
a cascade removes rows whose FK *equals* the deleted key, and `NULL` never equals anything.

`Notification` having no `UserId` is load-bearing here. Keeping one and cascading it from
`sec.User` would create both `User → Notification → NotificationRead` and `User → NotificationRead`
— two cascade paths, SQL Server error 1785.

## Not included

No delivery channels, no templates, no push transport. Display text is the client's, rendered from
`Type` + `Data`; no display string is ever stored.
