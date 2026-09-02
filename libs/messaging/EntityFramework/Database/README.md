# Sencilla.Messaging.EntityFramework.Mssql

SQL Server schema for the durable Entity Framework messaging transport: the `[Message]` queue
table, in `[dbo]`.

The schema **source** ships under `sql/` and the package's `build/*.props` compiles it into the
consuming database's own model — the table lands physically in your dacpac, no composite reference
and no co-located dacpac to deploy.

## Using it

Reference the package from your SQL project. Nothing else: there is no seed data and no
post-deployment include, because there are no lookup tables. `MessageState` is a code enum
documented on the column, and the payload type is a string rather than an app-defined lookup —
adding a message type touches no SQL at all.

`Message.UserId` carries a FK to `[sec].[User]`, so `Sencilla.Component.Users.Mssql` is referenced
for standalone build and validation.

## Shape

| Column group | Purpose |
| --- | --- |
| `Stream`, `PayloadType`, `Namespace`, `Name` | routing and type resolution |
| `Payload`, `Metadata`, `CorrelationId`, `EntityId`, `UserId` | the message itself |
| `State`, `Attempts`, `AvailableAt`, `Error`, `ProcessedAt` | lifecycle and retry |
| `ProcessService`, `ProcessStartDate`, `RowVersion` | the claim contract |

Two indexes matter: `IX_Message_Claim` serves the claim query (`Stream` first, so consumers never
compete for each other's rows) and `IX_Message_EntityId` answers "is anything queued for this
entity?" without scanning payload JSON.
