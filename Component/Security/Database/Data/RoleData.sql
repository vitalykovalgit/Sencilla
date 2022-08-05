
SET IDENTITY_INSERT [sec].[Role] ON

DECLARE @Role_Root      INT = 1,
        @Role_Anonymous INT = 2,
        @Role_User      INT = 3,
        @Role_Guest     INT = 4,
        @Role_Admin     INT = 5

MERGE INTO [sec].[Role] AS Target
USING 
(
	VALUES
    (@Role_Root,      N'Root'),
    (@Role_Anonymous, N'Anonymous'),
    (@Role_User,      N'User'),
    (@Role_Guest,     N'Guest'),
    (@Role_Admin,     N'Admin')
)
AS Source([Id], [Name])

ON Target.[Id] = Source.[Id]
WHEN MATCHED THEN UPDATE SET Target.[Name] = Source.[Name]
WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Name]) VALUES (Source.[Id], Source.[Name])
WHEN NOT MATCHED BY SOURCE THEN DELETE;

SET IDENTITY_INSERT [sec].[Role] OFF
