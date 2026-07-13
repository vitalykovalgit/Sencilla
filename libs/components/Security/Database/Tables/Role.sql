CREATE TABLE [sec].[Role]
(
	[Id]     INT            NOT NULL IDENTITY(100, 1),
    [Name]   NVARCHAR(255)  NULL,
    -- Inheritance: this role additionally grants everything its parent grants
    -- (transitive; e.g. Owner ⊃ Editor ⊃ Viewer). A role's SCOPE is not stored here —
    -- it is where the role is held: globally (sec.UserRole) or on a row ([SecureWith]
    -- role tables such as ProjectUser).
    [Parent] INT            NULL,
    CONSTRAINT [PK_Role]        PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Role_Parent] FOREIGN KEY ([Parent]) REFERENCES [sec].[Role]([Id]),
)
