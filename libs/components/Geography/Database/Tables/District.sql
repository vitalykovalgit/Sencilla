CREATE TABLE [geo].[District]
(
	[Id]         INT IDENTITY    NOT NULL,
    [CountryId]  INT             NOT NULL,
    [RegionId]   INT             NOT NULL,
    [Name]       NVARCHAR(255)   NOT NULL,
    [Code]       NVARCHAR(55)    NULL,
    [PhoneCode]  NVARCHAR(55)    NULL,
    [PostalCode] NVARCHAR(10)    NULL,

    CONSTRAINT [PK_District] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_District_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [geo].[Country](Id),
    CONSTRAINT [FK_District_RegionId]  FOREIGN KEY ([RegionId])  REFERENCES [geo].[Region](Id),
)
