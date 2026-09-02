-- Per-user read state.
--
-- [NotificationId] NULL is a WATERMARK: the user has read everything created at or before
-- [ReadDate]. That is what the bell dropdown writes — one row per user, not one per notification
-- per user — and it also covers rows past the loaded page, which a per-row stamp never did.
-- Per-notification rows are supported by the schema for a future per-item "mark read" action;
-- nothing writes them today.
CREATE TABLE [NotificationRead]
(
    [Id]             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [NotificationId] UNIQUEIDENTIFIER NULL, -- NULL = watermark
    [UserId]         UNIQUEIDENTIFIER NOT NULL,

    -- Server-stamped (IEntityCreateableTrack). NEVER client-supplied: notification CreatedDate
    -- comes from the worker's clock, so a browser clock running fast would set a watermark into
    -- the future and silently mark notifications read before they are even written.
    [ReadDate]       DATETIME2        NOT NULL,

    CONSTRAINT [PK_NotificationRead] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- Cascade so retention stays a single DELETE against [Notification]. Watermark rows are
    -- immune for free: cascade removes rows whose FK EQUALS the deleted key, and NULL never
    -- equals anything — no WHERE clause for a future refactor to forget.
    CONSTRAINT [FK_NotificationRead_NotificationId] FOREIGN KEY ([NotificationId])
        REFERENCES [Notification]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_NotificationRead_UserId] FOREIGN KEY ([UserId])
        REFERENCES [sec].[User](Id) ON DELETE CASCADE,

    -- Watermarks are APPEND-ONLY and the inbox view takes MAX(ReadDate); superseded ones are
    -- swept by the app's retention job.
    --
    -- A filtered UNIQUE on ([UserId]) WHERE [NotificationId] IS NULL would bound the set to one
    -- row per user, but then marking read a second time is a duplicate-key 500 unless the write
    -- becomes an upsert — and an upsert needs either an Update grant (a CrudApi update is a
    -- full-row replace, and this is the one row a user could usefully forge) or a security
    -- bypass inside a request handler. Appending keeps the write a plain create that the normal
    -- post-image check already scopes, at the cost of a sweep.
    -- ReadDate is a key column rather than an INCLUDE because the inline CREATE TABLE index
    -- grammar accepts a WHERE filter but not INCLUDE. It covers the view's OUTER APPLY either way.
    INDEX [IX_NotificationRead_UserId] NONCLUSTERED ([UserId], [NotificationId], [ReadDate]),
);
