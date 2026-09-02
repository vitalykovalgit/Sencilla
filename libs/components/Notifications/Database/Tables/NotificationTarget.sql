-- Who receives a notification. Rows UNION (each row widens the audience, matching how security
-- matrix grants accumulate); the slots WITHIN one row AND, so "Owners of project X" is a single
-- row rather than something union semantics cannot express.
CREATE TABLE [NotificationTarget]
(
    -- Sequential GUID like its two siblings. An IDENTITY key would save bytes on a table that is
    -- never addressed by the API, but it also drags in EF's identity-column handling for no gain.
    [Id]             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [NotificationId] UNIQUEIDENTIFIER NOT NULL,

    -- 1 User, 2 Role, 3 System (everybody). 100+ are app-defined domain scopes; the component
    -- resolves 1-3 in [NotificationInboxView] and knows nothing about the rest.
    [Scope]          TINYINT          NOT NULL,

    -- Type-tagged target slots; [Scope] says which participate. Two typed columns rather than one
    -- polymorphic id because sec.Role.Id is INT while user/entity keys are GUIDs, and rather than
    -- the stringified (EntityType, EntityId) pattern used by audit/tags because those columns are
    -- only ever FILTERED as strings, whereas these are JOINED back to typed keys on every poll —
    -- a string would force a non-sargable cast into the join predicate.
    [TargetGuid]     UNIQUEIDENTIFIER NULL, -- scope 1: UserId. App scopes: their own entity id.
    [TargetInt]      INT              NULL, -- scope 2: sec.Role.Id

    CONSTRAINT [PK_NotificationTarget] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- Cascade so retention stays a single DELETE against [Notification].
    CONSTRAINT [FK_NotificationTarget_NotificationId] FOREIGN KEY ([NotificationId])
        REFERENCES [Notification]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_NotificationTarget_TargetInt] FOREIGN KEY ([TargetInt]) REFERENCES [sec].[Role](Id),

    -- No FK on TargetGuid: it is polymorphic, and app scopes point at tables in components this
    -- one does not depend on.

    -- A row whose discriminator and slots disagree joins to no branch of the view and therefore
    -- reaches NOBODY — silently, with no error and no log. This constraint is the cheapest
    -- possible test of every write path.
    CONSTRAINT [CK_NotificationTarget_Scope] CHECK (
           ([Scope] = 1   AND [TargetGuid] IS NOT NULL)
        OR ([Scope] = 2   AND [TargetInt]  IS NOT NULL)
        OR ([Scope] = 3)
        OR ([Scope] >= 100 AND [TargetGuid] IS NOT NULL)
    ),

    -- One index per resolvable branch, filtered: three of four key slots are NULL on most rows.
    INDEX [IX_NotificationTarget_User]   NONCLUSTERED ([TargetGuid], [NotificationId]) WHERE [Scope] = 1,
    INDEX [IX_NotificationTarget_Role]   NONCLUSTERED ([TargetInt],  [NotificationId]) WHERE [Scope] = 2,
    INDEX [IX_NotificationTarget_System] NONCLUSTERED ([NotificationId])               WHERE [Scope] = 3,
);
