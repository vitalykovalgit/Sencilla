CREATE TABLE [sec].[RefreshToken]
(
    [Id]         NVARCHAR (128)   NOT NULL,
    [UserId]     UNIQUEIDENTIFIER NOT NULL,
    [FamilyId]   UNIQUEIDENTIFIER NOT NULL,
    [ExpiresAt]  DATETIME2        NOT NULL,
    [RedeemedAt] DATETIME2        NULL,
    [Revoked]    BIT              NOT NULL CONSTRAINT [DF_RefreshToken_Revoked] DEFAULT 0,

    CONSTRAINT [PK_RefreshToken] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- UserId is a logical reference to sec.User — no FK constraint declared so this project
    -- compiles standalone without a Users dependency (same pattern as Files.Mssql).
    INDEX [IX_RefreshToken_UserId]   NONCLUSTERED ([UserId]),
    INDEX [IX_RefreshToken_FamilyId] NONCLUSTERED ([FamilyId])
)
