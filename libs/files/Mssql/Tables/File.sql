CREATE TABLE [File]
(
    [Id]       UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWID(),
    [ParentId] UNIQUEIDENTIFIER  NULL,

    [Type]   SMALLINT NOT NULL DEFAULT 0,
    [Status] SMALLINT NOT NULL DEFAULT 1,

    [Name]     NVARCHAR(1024)   NOT NULL,
    [MimeType] NVARCHAR(250)    NULL,
    [Size]     BIGINT           NOT NULL,
    [Uploaded] BIGINT           NOT NULL DEFAULT 0,

    [Origin]  INT     NOT NULL DEFAULT 0,
    [Storage] TINYINT NOT NULL DEFAULT 0,
    [Path]    NVARCHAR(4000)   NULL,

    [UserId] UNIQUEIDENTIFIER NULL,

    [Dim]    INT NULL,
    [Width]  INT NULL,
    [Height] INT NULL,

    [Res]   NVARCHAR(4000) NULL,
    [Attrs] NVARCHAR(4000) NULL,

    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [DeletedDate] DATETIME2 NULL,

    CONSTRAINT [FK_File_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [File]([Id]),
    CONSTRAINT [FK_File_Status]   FOREIGN KEY ([Status])   REFERENCES [FileStatus]([Id]),

    INDEX [IX_File_ParentId_Dim] NONCLUSTERED ([ParentId], [Dim])
)
