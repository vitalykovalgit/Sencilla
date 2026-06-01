CREATE TABLE [sec].[UserAddressType] 
(
	[Id]     TINYINT IDENTITY NOT NULL,
    [Name]   NVARCHAR(255)    NOT NULL,
	
	CONSTRAINT [PK_UserAddressType] PRIMARY KEY CLUSTERED ([Id] ASC),	
    CONSTRAINT [UC_UserAddressType_Name] UNIQUE ([Name])
)