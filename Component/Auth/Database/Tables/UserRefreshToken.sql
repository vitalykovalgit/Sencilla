CREATE TABLE [sec].[UserRefreshToken]
(
    [Id]               INT IDENTITY NOT NULL,
    [UserId]           INT NOT NULL,
    [Token]            NVARCHAR(2000) NOT NULL,
    [ExpiresAt]        DATETIME2(7) NOT NULL,
    [CreatedAt]        DATETIME2(7) NOT NULL,
    [CreatedByIp]      NVARCHAR(45)  NULL,
    [RevokedAt]        DATETIME2(7)  NULL,
    [RevokedByIp]      NVARCHAR(45)  NULL,
    [ReplacedByToken]  NVARCHAR(2000) NULL,

    CONSTRAINT [PK_UserRefreshToken] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserRefreshToken_UserId] 
        FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id])
)
