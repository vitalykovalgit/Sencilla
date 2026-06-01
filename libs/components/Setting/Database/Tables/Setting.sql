CREATE TABLE [dbo].[Setting]
(
    [Id]          INT IDENTITY    NOT NULL,
    [UserId]      INT             NULL,
    [Type]        INT             NOT NULL,
    [ParentId]    INT             NULL,
    [Name]        NVARCHAR(300)   NULL,
    [Desc]        NVARCHAR(1000)  NULL,
    [Value]       NVARCHAR(300)   NULL,

    CONSTRAINT [PK_Setting] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- TODO(polish): wire this FK once Sencilla.Component.Users.Mssql exists and is
    -- referenced as a same-database package reference. Until then [UserId] is left
    -- unconstrained so this dacpac builds standalone.
    -- CONSTRAINT [FK_Setting_UserId] FOREIGN KEY ([UserId]) REFERENCES [sec].[User]([Id])
);
