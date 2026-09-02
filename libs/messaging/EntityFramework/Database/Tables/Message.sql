-- Durable message queue. One row per message; the Process* columns plus RowVersion are the claim
-- contract (optimistic claim, startup reclaim, age-based rescue — see EfMessageStream).
CREATE TABLE [Message]
(
    [Id]     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(), -- app-generated sequential GUID; DB default is a fallback for non-EF inserts

    -- Logical queue. Consumers claim by it, so it leads the claim index: without it every
    -- consumer on the database would compete for every other consumer's rows.
    [Stream] NVARCHAR(100) NOT NULL,

    -- Resolution key for the payload type: a [PayloadType] alias when the type declares one, else
    -- its .NET FullName. Persisted forever, so it must not be assembly-qualified.
    [PayloadType] NVARCHAR(255) NOT NULL,
    [Namespace]   NVARCHAR(500) NULL,  -- payload FullName, diagnostics only
    [Name]        NVARCHAR(255) NULL,  -- short type name, diagnostics only

    [CorrelationId] UNIQUEIDENTIFIER NULL,

    -- Sencilla.Messaging.MessageState: 0 New, 1 InProgress, 2 Succeeded, 3 Failed.
    [State] TINYINT NOT NULL DEFAULT 0,

    [Payload]  NVARCHAR(MAX) NULL, -- serialized payload (camelCase, see MessageJson)
    [Metadata] NVARCHAR(MAX) NULL, -- serialized envelope metadata dictionary

    -- Subject entity of this message, if any. No foreign key on purpose: a message may outlive its
    -- subject row (a delete message outlives what it deletes).
    [EntityId] UNIQUEIDENTIFIER NULL,
    [UserId]   UNIQUEIDENTIFIER NULL, -- initiator, when there is one

    [Attempts]    INT           NOT NULL DEFAULT 0,
    [AvailableAt] DATETIME2     NULL,     -- claimable from; null = immediately. Retry backoff and delays
    [Error]       NVARCHAR(MAX) NULL,     -- error CODE + detail (codes, not display strings)

    [CreatedAt]   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ProcessedAt] DATETIME2 NULL,

    [ProcessService]   VARCHAR(255) NULL, -- claiming instance; null when unclaimed
    [ProcessStartDate] DATETIME2    NULL,

    [RowVersion] ROWVERSION NOT NULL,

    -- constraints
    CONSTRAINT [PK_Message]        PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Message_UserId] FOREIGN KEY ([UserId]) REFERENCES sec.[User](Id),

    -- The claim query: WHERE Stream = @s AND State = 0 AND (AvailableAt IS NULL OR <= now) ORDER BY CreatedAt.
    INDEX [IX_Message_Claim]    NONCLUSTERED ([Stream] ASC, [State] ASC, [AvailableAt] ASC, [CreatedAt] ASC),
    -- "Is anything queued for this entity?" — the dedupe question every enqueuing scanner asks.
    INDEX [IX_Message_EntityId] NONCLUSTERED ([EntityId] ASC) INCLUDE ([PayloadType], [State]),
)
