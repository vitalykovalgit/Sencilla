CREATE TABLE [sec].[Action] 
(
	[Id]    INT             IDENTITY NOT NULL,
    [Name]  NVARCHAR (255)  NULL,
    CONSTRAINT [PK_Action]        PRIMARY KEY CLUSTERED ([Id] ASC),
)