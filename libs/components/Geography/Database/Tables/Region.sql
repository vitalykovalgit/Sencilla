CREATE TABLE [geo].[Region]
(
	[Id]        INT IDENTITY    NOT NULL,
    [CountryId] INT             NOT NULL,
    [Name]      NVARCHAR(255)   NOT NULL,
    [Code]      NVARCHAR(55)    NULL,
    [PhoneCode] NVARCHAR(55)    NULL

    CONSTRAINT [PK_Region] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Region_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [geo].[Country](Id),
)
