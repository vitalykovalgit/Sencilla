CREATE TABLE [sec].[UserAddress] 
(
	[Id]          INT IDENTITY NOT NULL,

    [UserId]      INT NOT NULL,
    [Type]        TINYINT NOT NULL DEFAULT 1,

    [CountryId]   INT NULL, 
    [StateId]     INT NULL,
    [RegionId]    INT NULL,
    [DistrictId]  INT NULL,
    [CityId]      INT NULL,

    [PostalCode]  NVARCHAR(12)  NULL,
    [Street]      NVARCHAR(255) NULL,
    [House]       NVARCHAR(10)  NULL,
    [Apart]       NVARCHAR(10)  NULL,

	[CreatedDate] DATETIME2  NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedDate] DATETIME2  NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_UserAddress]        PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserAddress_UserId] FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserAddress_Type]   FOREIGN KEY ([Type])   REFERENCES [sec].[UserAddressType]([Id]),
)