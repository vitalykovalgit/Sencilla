SET IDENTITY_INSERT [FileStatus] ON

MERGE [FileStatus] AS Target
USING (VALUES
    (1, N'Ready'),
    (2, N'Processing'),
    (3, N'System'),
    (4, N'Draft'),
    (5, N'Temporary')
)
AS Source([Id], [Name]) ON Target.[Id] = Source.[Id]

WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Name]) VALUES (Source.[Id], Source.[Name])
WHEN MATCHED               THEN UPDATE SET Target.[Name] = Source.[Name]
WHEN NOT MATCHED BY SOURCE THEN DELETE;

SET IDENTITY_INSERT [FileStatus] OFF
