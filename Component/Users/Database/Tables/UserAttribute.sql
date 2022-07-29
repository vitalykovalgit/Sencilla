CREATE TABLE [sec].[UserAttribute] 
(
	[Id]                INT             IDENTITY NOT NULL,
	[UserId]            INT             NOT NULL,
    [Name]              NVARCHAR (255)  NULL,
    [Value]             NVARCHAR (1024) NULL,
    
	[CreatedDate]       DATETIME2 (7)  NOT NULL,
    [UpdatedDate]       DATETIME2 (7)  NOT NULL,

    CONSTRAINT [PK_UserAttribute]        PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserAttribute_UserId] FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id])
)