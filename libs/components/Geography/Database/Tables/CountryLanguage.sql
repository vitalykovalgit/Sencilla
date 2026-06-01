CREATE TABLE [geo].[CountryLanguage]
(
	[Id]			INT IDENTITY    NOT NULL,

	[CountryId]		INT	NOT NULL,
	[LanguageId]	INT	NOT NULL,

    CONSTRAINT [PK_CountryLanguage] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_CountryLanguage_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [geo].[Country](Id),
    CONSTRAINT [FK_CountryLanguage_LanguageId] FOREIGN KEY ([LanguageId]) REFERENCES [geo].[Language](Id),
)
