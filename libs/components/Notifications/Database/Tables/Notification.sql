-- In-app notifications. Written only by trusted app code (workers, endpoints); clients never
-- touch this table — they read the flattened [NotificationInboxView] and mark rows read by
-- inserting into [NotificationRead].
--
-- Deliberately has NO UserId: audience lives in [NotificationTarget], so one row serves one
-- recipient, a role, or everybody. Dropping the column is also what keeps the cascades on the
-- two child tables legal — sec.User -> Notification -> NotificationRead plus
-- sec.User -> NotificationRead would be two cascade paths (SQL Server error 1785).
CREATE TABLE [Notification]
(
    [Id]   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(), -- app-generated sequential GUID (EF SequentialGuidValueGenerator); DB default is a fallback for non-EF inserts

    -- Wire key for the notification class: its [NotificationType] alias, else the class name
    -- without namespace (see NotificationType.KeyOf). Persisted forever, so prefer an explicit
    -- alias over a class name you may want to rename later. No lookup table and no FK: the
    -- notification classes ARE the registry, and they are compile-time checked at every write.
    [Type] NVARCHAR(100) NOT NULL,

    [Data] NVARCHAR(MAX) NULL, -- ids + error CODES only, client renders all text

    [CreatedDate] DATETIME2 NOT NULL,

    CONSTRAINT [PK_Notification] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- The inbox query orders newest-first over the audience join: createdDate|desc, take 50.
    INDEX [IX_Notification_CreatedDate] NONCLUSTERED ([CreatedDate] DESC),
);
