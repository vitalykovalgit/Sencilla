CREATE TABLE [sec].[UserAuth] 
(
	[Id]                TINYINT IDENTITY NOT NULL,
	[UserId]            INT              NOT NULL,
    [Auth]              NVARCHAR (100)   NOT NULL,
    [Email]             NVARCHAR (255)   NOT NULL,
    [PasswordHash]      NVARCHAR (MAX)   NULL,
	[CreatedDate]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_UserAuth]         PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserAuth_UserId]  FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id]) ON DELETE CASCADE,

    CONSTRAINT [UC_UserAuth_UserIdEmailAuth] UNIQUE ([UserId],[Auth],[Email])
)