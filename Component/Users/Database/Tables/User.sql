
CREATE TABLE [sec].[User] 
(
	[Id]                INT             IDENTITY NOT NULL,
    [FirstName]         NVARCHAR (255)  NOT NULL,
    [LastName]          NVARCHAR (255)  NULL,
    [FatherName]        NVARCHAR (255)  NULL,

    [Email]             NVARCHAR (255)  NOT NULL,
    [Phone]             BIGINT NULL,
    
	[CreatedDate]       DATETIME2 (7)  NOT NULL,
    [UpdatedDate]       DATETIME2 (7)  NOT NULL,
    [DeletedDate]       DATETIME2 (7)  NULL,

    CONSTRAINT [PK_User]       PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UC_User_Phone] UNIQUE ([Phone]),
    CONSTRAINT [UC_User_Email] UNIQUE ([Email])
)
