CREATE TABLE [sec].[PasswordResetToken]
(
    [Id]        UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_PasswordResetToken_Id] DEFAULT NEWSEQUENTIALID(),
    [UserId]    UNIQUEIDENTIFIER NOT NULL,
    [TokenHash] NVARCHAR (64)    NOT NULL,
    [ExpiresAt] DATETIME2        NOT NULL,
    [UsedAt]    DATETIME2        NULL,

    CONSTRAINT [PK_PasswordResetToken] PRIMARY KEY CLUSTERED ([Id] ASC),

    INDEX [IX_PasswordResetToken_UserId]    NONCLUSTERED ([UserId]),
    INDEX [IX_PasswordResetToken_TokenHash] NONCLUSTERED ([TokenHash])
)
