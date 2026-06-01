CREATE TABLE [geo].[Language]
(
	[Id]    INT IDENTITY    NOT NULL,
    [Name]  NVARCHAR(255)   NOT NULL,
    [Code]  NVARCHAR(55)    NOT NULL,

    CONSTRAINT [PK_Language]  PRIMARY KEY CLUSTERED ([Id] ASC),
)
