CREATE TABLE [geo].[State]
(
	[Id]         INT IDENTITY    NOT NULL,
    [CountryId]  INT             NOT NULL,
    [Name]       NVARCHAR(255)   NOT NULL,
    [Code]       NVARCHAR(55)    NULL,
    [PhoneCode]  NVARCHAR(55)    NULL,
    [PostalCode] NVARCHAR(10)    NULL,

    CONSTRAINT [PK_State] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_State_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [geo].[Country](Id),
)
