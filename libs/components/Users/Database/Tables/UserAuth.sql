CREATE TABLE [sec].[UserAuth] 
(
	[Id]                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
	[UserId]            UNIQUEIDENTIFIER NOT NULL,
    [Auth]              NVARCHAR (100)   NOT NULL,
    [ProviderKey]       NVARCHAR (255)   NULL,
    [Email]             NVARCHAR (255)   NULL,
    [PasswordHash]      NVARCHAR (512)   NULL,
	[CreatedDate]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_UserAuth]         PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserAuth_UserId]  FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id]) ON DELETE CASCADE,

    -- One provider subject maps to exactly one user globally. Filtered so multiple rows may have a
    -- null ProviderKey during transition.
    INDEX [UX_UserAuth_AuthProviderKey] UNIQUE NONCLUSTERED ([Auth], [ProviderKey]) WHERE [ProviderKey] IS NOT NULL,

    -- The key UserRegistrationMiddleware matches a sign-in on (MERGE … WITH (HOLDLOCK) ON Email, Auth, UserId).
    -- Without a unique index under it, the HOLDLOCK range-locks whatever index the plan picks, and the burst
    -- of parallel requests a first page load fires deadlocked on IX_UserAuth_UserId (1205 → 500 → the SPA
    -- looked signed out). Unique, those requests queue on one key instead. Email FIRST: user ids are
    -- sequential, so a UserId-led key would put every new user in the same last gap of the index.
    INDEX [UX_UserAuth_EmailAuthUser] UNIQUE NONCLUSTERED ([Email], [Auth], [UserId]),
    INDEX [IX_UserAuth_UserId] NONCLUSTERED ([UserId])
)