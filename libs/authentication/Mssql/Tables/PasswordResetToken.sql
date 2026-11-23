CREATE TABLE [sec].[PasswordResetToken]
(
    [Id]        UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_PasswordResetToken_Id] DEFAULT NEWSEQUENTIALID(),
    [UserId]    UNIQUEIDENTIFIER NOT NULL,
    [TokenHash] NVARCHAR (64)    NOT NULL,
    [ExpiresAt] DATETIME2        NOT NULL,
    [UsedAt]    DATETIME2        NULL,
    -- 'reset' or 'verify' — a reset link can never confirm an address and vice versa.
    [Purpose]   NVARCHAR (16)    NOT NULL CONSTRAINT [DF_PasswordResetToken_Purpose] DEFAULT ('reset'),
    -- The address a 'verify' token proves when it is not yet the account's; NULL = the account's own.
    [Email]     NVARCHAR (255)   NULL,

    CONSTRAINT [PK_PasswordResetToken] PRIMARY KEY CLUSTERED ([Id] ASC),

    INDEX [IX_PasswordResetToken_UserId]    NONCLUSTERED ([UserId]),
    INDEX [IX_PasswordResetToken_TokenHash] NONCLUSTERED ([TokenHash])
)
