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
    INDEX [IX_UserAuth_UserId] NONCLUSTERED ([UserId])
)