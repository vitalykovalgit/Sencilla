
SET IDENTITY_INSERT [sec].[Area] ON

DECLARE @AreaApp  INT = 1,
        @AreaUser INT = 2

MERGE INTO [sec].[Area] AS Target
USING 
(
	VALUES
    (@AreaApp,    N'App'),
    (@AreaUser,   N'User')
)
AS Source([Id], [Name])

ON Target.[Id] = Source.[Id]
WHEN MATCHED THEN UPDATE SET Target.[Name] = Source.[Name]
WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Name]) VALUES (Source.[Id], Source.[Name])
WHEN NOT MATCHED BY SOURCE THEN DELETE;

SET IDENTITY_INSERT [sec].[Area] OFF
