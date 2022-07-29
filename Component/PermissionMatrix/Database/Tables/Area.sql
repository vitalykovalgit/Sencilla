CREATE TABLE [sec].[Area] 
(
	[Id]    INT IDENTITY   NOT NULL,
    [Name]  NVARCHAR(255)  NULL,
    CONSTRAINT [PK_UserArea]        PRIMARY KEY CLUSTERED ([Id] ASC),
)
