CREATE TABLE [geo].[City]
(
	[Id]         INT IDENTITY    NOT NULL,
    [CountryId]  INT             NOT NULL,
    [StateId]    INT             NULL,
    [RegionId]   INT             NULL,
    [DistrictId] INT           NULL,
    [Name]       NVARCHAR(255)   NOT NULL,
    [Code]       NVARCHAR(55)    NULL,
    [PhoneCode]  NVARCHAR(55)    NULL,
    [PostalCode] NVARCHAR(10)    NULL,

    CONSTRAINT [PK_City] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_City_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [geo].[Country](Id),
    CONSTRAINT [FK_City_RegionId]  FOREIGN KEY ([RegionId])  REFERENCES [geo].[Region](Id),
    CONSTRAINT [FK_City_StateId]   FOREIGN KEY ([StateId])   REFERENCES [geo].[State](Id),
)
