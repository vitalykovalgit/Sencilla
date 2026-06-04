CREATE TABLE [sec].[UserContact] 
(
	[Id]                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
	[UserId]            UNIQUEIDENTIFIER NOT NULL,
    [Type]              TINYINT       NOT NULL,
    [Contact]             NVARCHAR(255) NOT NULL,
    [Order]             TINYINT       NOT NULL DEFAULT 0,
	[CreatedDate]       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_UserContact]        PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserContact_UserId] FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserContact_Type]   FOREIGN KEY ([Type])   REFERENCES [sec].[UserContactType]([Id]),

    CONSTRAINT [UC_UserContact_UserIdTypePhone] UNIQUE ([UserId],[Type],[Contact])
)