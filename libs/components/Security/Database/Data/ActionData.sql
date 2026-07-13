
SET IDENTITY_INSERT [sec].[Action] ON

-- Actions are bit flags (Read=1, Create=2, Update=4, Delete=8) and
-- [sec].[Matrix].[Action] carries FK_Matrix_Action, so every legal combination
-- is seeded — a Matrix row may grant any subset of operations.
MERGE INTO [sec].[Action] AS Target
USING
(
	VALUES
    ( 1, N'Read'),
    ( 2, N'Create'),
    ( 3, N'Read, Create'),
    ( 4, N'Update'),
    ( 5, N'Read, Update'),
    ( 6, N'Create, Update'),
    ( 7, N'Read, Create, Update'),
    ( 8, N'Delete'),
    ( 9, N'Read, Delete'),
    (10, N'Create, Delete'),
    (11, N'Read, Create, Delete'),
    (12, N'Update, Delete'),
    (13, N'Read, Update, Delete'),
    (14, N'Create, Update, Delete'),
    (15, N'All')
)
AS Source([Id], [Name])

ON Target.[Id] = Source.[Id]
WHEN MATCHED THEN UPDATE SET Target.[Name] = Source.[Name]
WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Name]) VALUES (Source.[Id], Source.[Name])
WHEN NOT MATCHED BY SOURCE THEN DELETE;

SET IDENTITY_INSERT [sec].[Action] OFF
