CREATE TABLE [geo].[Country]
(
	[Id]        INT IDENTITY    NOT NULL,
    [Name]      NVARCHAR(255)   NOT NULL,
    [Iso2]      NVARCHAR(55)    NOT NULL,
    [Iso3]      NVARCHAR(55)    NOT NULL,
    [Code]      NVARCHAR(55)    NOT NULL,
    [PhoneCode] NVARCHAR(55)    NOT NULL,
    [UsaName]   NVARCHAR(255)   NULL,

    CONSTRAINT [PK_Country] PRIMARY KEY CLUSTERED ([Id] ASC),
)
