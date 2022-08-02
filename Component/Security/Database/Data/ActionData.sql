
SET IDENTITY_INSERT [sec].[Action] ON

DECLARE @ActionRead INT = 1,
        @ActionCreate INT = 2,
        @ActionUpdate INT = 3,
        @ActionDelete INT = 4

MERGE INTO [sec].[Action] AS Target
USING 
(
	VALUES
    (@ActionRead,    N'Read'),
    (@ActionCreate,  N'Create'),
    (@ActionUpdate,  N'Update'),
    (@ActionDelete,  N'Delete')
) 
AS Source([Id], [Name])

ON Target.[Id] = Source.[Id]
WHEN MATCHED THEN UPDATE SET Target.[Name] = Source.[Name]
WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Name]) VALUES (Source.[Id], Source.[Name])
WHEN NOT MATCHED BY SOURCE THEN DELETE;

SET IDENTITY_INSERT [sec].[Action] OFF
