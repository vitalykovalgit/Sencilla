
CREATE TABLE [sec].[User] 
(
	[Id]                INT    IDENTITY NOT NULL,

    [Phone]             BIGINT NULL,
    [PhoneConf]         BIT NOT NULL DEFAULT 0,

    [Email]             NVARCHAR (255)  NOT NULL,
    [EmailConf]         BIT NOT NULL DEFAULT 0,

    [FirstName]         NVARCHAR (255)  NULL,
    [LastName]          NVARCHAR (255)  NULL,
    [FatherName]        NVARCHAR (255)  NULL,

    [Pic]               NVARCHAR (1024) NULL,

    [Gender]            TINYINT NULL,
    [Status]            TINYINT NULL,
    [Type]              TINYINT NULL,

    [Comments]          NVARCHAR(1024) NULL,
    [Attrs]             NVARCHAR(4000) NULL,
    
	[BirthDate]         DATETIME2  NULL,
	[CreatedDate]       DATETIME2  NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedDate]       DATETIME2  NOT NULL DEFAULT GETUTCDATE(),
    [DeletedDate]       DATETIME2  NULL,

    CONSTRAINT [PK_User]         PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_User_Type]    FOREIGN KEY ([Type])   REFERENCES [sec].[UserType](Id),
    CONSTRAINT [FK_User_Status]  FOREIGN KEY ([Status]) REFERENCES [sec].[UserStatus](Id),
    CONSTRAINT [FK_User_Gender]  FOREIGN KEY ([Gender]) REFERENCES [sec].[UserGender](Id),

	-- it can come as 0 let's disable for now 
		--CONSTRAINT [UC_User_Phone] UNIQUE ([Phone]),
		CONSTRAINT [UC_User_Email] UNIQUE ([Email]),

		INDEX [IX_User_Email] NONCLUSTERED ([Email])
	)
