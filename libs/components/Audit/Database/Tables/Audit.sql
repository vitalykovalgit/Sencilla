CREATE TABLE [audit].[Audit]
(
    [Id]            BIGINT IDENTITY  NOT NULL,

    [EntityType]    NVARCHAR(256)    NOT NULL,          -- audited entity type name (e.g. 'Order')
    [EntityId]      NVARCHAR(64)     NOT NULL,          -- audited row id, type-agnostic (Guid or int as string)
    [Action]        TINYINT          NOT NULL,          -- FK audit.AuditAction: 1 Insert, 2 Update, 3 Delete

    [ActorType]     TINYINT          NOT NULL,          -- FK audit.ActorType: 0 System, 1 User, 2 Admin
    [ActorId]       UNIQUEIDENTIFIER NULL,              -- acting user's id (null for System)

    [Changes]       NVARCHAR(MAX)    NULL,              -- {field:{old,new}} JSON (insert: old null; delete: new null)
    [Reason]        NVARCHAR(1024)   NULL,              -- optional X-Audit-Reason
    [CorrelationId] UNIQUEIDENTIFIER NOT NULL,          -- groups one logical operation's rows

    [CreatedDate]   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_Audit] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- Enum-value integrity: [Action]/[ActorType] must be a seeded lookup row. Safe inline (audit.Audit
    -- is append-only and empty when a consumer first compiles this component in), and the lookups are
    -- seeded in post-deployment (audit.Audit is never written before deployment completes).
    CONSTRAINT [FK_Audit_Action]    FOREIGN KEY ([Action])    REFERENCES [audit].[AuditAction]([Id]),
    CONSTRAINT [FK_Audit_ActorType] FOREIGN KEY ([ActorType]) REFERENCES [audit].[ActorType]([Id]),

    -- per-entity history (EntityType, EntityId, time) and cross-entity operation lookup (CorrelationId).
    INDEX [IX_Audit_Entity]      NONCLUSTERED ([EntityType] ASC, [EntityId] ASC, [CreatedDate] ASC),
    INDEX [IX_Audit_Correlation] NONCLUSTERED ([CorrelationId] ASC),
)
