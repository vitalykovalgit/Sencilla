CREATE TABLE [sec].[Role] 
(
	[Id]    INT            NOT NULL IDENTITY(100, 1),
    [Name]  NVARCHAR(255)  NULL,
    CONSTRAINT [PK_Role]   PRIMARY KEY CLUSTERED ([Id] ASC),
)
