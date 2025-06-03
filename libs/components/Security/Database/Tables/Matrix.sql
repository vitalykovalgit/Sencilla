CREATE TABLE [sec].[Matrix] 
(
	[Id]                INT            IDENTITY NOT NULL,
    [Role]              INT            NOT NULL, 
    [Resource]          VARCHAR (255)  NOT NULL,
    --[Area]              INT            NOT NULL,
    [Action]            INT            NOT NULL,
    [Constraint]        NVARCHAR(1024) NULL,

    CONSTRAINT [PK_Matrix]        PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Matrix_Role]   FOREIGN KEY ([Role])   REFERENCES [sec].[Role]([Id]),
    CONSTRAINT [FK_Matrix_Action] FOREIGN KEY ([Action]) REFERENCES [sec].[Action]([Id])
)