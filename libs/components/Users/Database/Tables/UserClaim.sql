CREATE TABLE [sec].[UserClaim] 
(
	[Id]                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
	[UserId]            UNIQUEIDENTIFIER NOT NULL,
    [Name]              NVARCHAR (255)  NOT NULL,
    [Value]             NVARCHAR (1024) NULL,
    
	[CreatedDate]       DATETIME2   NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedDate]       DATETIME2   NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_UserClaim]        PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserClaim_UserId] FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id])  ON DELETE CASCADE
)