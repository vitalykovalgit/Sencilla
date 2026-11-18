CREATE TABLE [sec].[UserStatus] 
(
	[Id]     TINYINT IDENTITY NOT NULL,
    [Name]   NVARCHAR(255)    NOT NULL,
    [NameKey] NVARCHAR(100)   NULL,
	
	CONSTRAINT [PK_UserStatus] PRIMARY KEY CLUSTERED ([Id] ASC),
	CONSTRAINT [UC_UserStatus_Name] UNIQUE ([Name])
)