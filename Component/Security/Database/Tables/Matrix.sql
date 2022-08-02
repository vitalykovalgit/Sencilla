CREATE TABLE [sec].[Matrix] 
(
	[Id]                INT            IDENTITY NOT NULL,
    [Role]              VARCHAR (255)  NOT NULL, /*Move to FK*/
    [Resource]          VARCHAR (255)  NOT NULL,
    [Area]              INT            NOT NULL,
    [Action]            INT            NOT NULL,

    CONSTRAINT [PK_Matrix]        PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Matrix_Action] FOREIGN KEY ([Action]) REFERENCES [sec].[Action]([Id])
)