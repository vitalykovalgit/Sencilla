# Sencilla.Component.Notifications.Mssql

SQL Server schema source for the Sencilla Notifications component: the in-app `Notification`
table and its `NotificationKind` lookup, in `[dbo]`, compiled into the consuming database's own
dacpac by the package's `build/*.props`.

Requires `Sencilla.Component.Users.Mssql` (the `Notification.UserId` FK targets `sec.User`).

No generic seed data ships: `NotificationKind` rows are app-defined — seed your own kinds (and
matrix grants for the `api/v1/notifications` surface, resource `notification`) in your own
post-deployment.
