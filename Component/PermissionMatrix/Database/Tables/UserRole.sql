CREATE TABLE [sec].[UserRole] 
(
	[Id]       INT IDENTITY   NOT NULL,
	[UserId]   INT IDENTITY   NOT NULL,
    [Role]     NVARCHAR(255)  NULL, /*TODO: Move to FK Id*/
    
	CONSTRAINT [PK_UserRole]        PRIMARY KEY CLUSTERED ([Id] ASC),
	CONSTRAINT [FK_UserRole_UserId] FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id])

)
