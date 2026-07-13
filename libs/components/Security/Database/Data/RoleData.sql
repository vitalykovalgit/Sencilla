
SET IDENTITY_INSERT [sec].[Role] ON

-- Built-in roles. Root/Anonymous/User are claims-derived and global-only; every other
-- role is holdable globally (sec.UserRole) or on a row ([SecureWith] role tables).
-- Owner ⊃ Editor ⊃ Viewer via the Parent chain (a role grants everything its parent grants).
MERGE INTO [sec].[Role] AS Target
USING
(
	VALUES
    (1, N'Root',      NULL),
    (2, N'Anonymous', NULL),
    (3, N'User',      NULL),
    (4, N'Guest',     NULL),
    (5, N'Admin',     NULL),
    (6, N'Owner',     7),
    (7, N'Editor',    8),
    (8, N'Viewer',    NULL)
)
AS Source([Id], [Name], [Parent])

ON Target.[Id] = Source.[Id]
WHEN MATCHED THEN UPDATE SET Target.[Name] = Source.[Name], Target.[Parent] = Source.[Parent]
WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Name], [Parent]) VALUES (Source.[Id], Source.[Name], Source.[Parent])
-- Only the seeded id range is authoritative: custom roles (IDENTITY starts at 100,
-- created by admins/deployments) must survive redeploys.
WHEN NOT MATCHED BY SOURCE AND Target.[Id] < 100 THEN DELETE;

SET IDENTITY_INSERT [sec].[Role] OFF
