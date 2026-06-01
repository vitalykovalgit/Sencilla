CREATE TABLE [sec].[UserContactType] 
(
	[Id]     TINYINT IDENTITY NOT NULL,
    [Name]   NVARCHAR(255)    NOT NULL,
	[Icon]   NVARCHAR(25)     NULL,
	[Order]  TINYINT          NOT NULL DEFAULT 0,
	[Hidden] BIT              NOT NULL DEFAULT 0,
	
	CONSTRAINT [PK_UserContactType] PRIMARY KEY CLUSTERED ([Id] ASC),	
    CONSTRAINT [UC_UserContactType_Name] UNIQUE ([Name])
)