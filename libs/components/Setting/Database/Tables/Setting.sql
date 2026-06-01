CREATE TABLE [dbo].[Setting]
(
    [Id]          INT IDENTITY    NOT NULL,
    [UserId]      UNIQUEIDENTIFIER NULL,
    [Type]        INT             NOT NULL,
    [ParentId]    INT             NULL,
    [Name]        NVARCHAR(300)   NULL,
    [Desc]        NVARCHAR(1000)  NULL,
    [Value]       NVARCHAR(300)   NULL,

    CONSTRAINT [PK_Setting] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- FK wired once [sec].[User].[Id] migrates to UNIQUEIDENTIFIER (parallel session).
    -- CONSTRAINT [FK_Setting_UserId] FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id])
);
