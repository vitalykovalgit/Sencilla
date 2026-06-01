IF NOT EXISTS (SELECT 1 FROM [sec].[UserType] WITH (UPDLOCK, HOLDLOCK))
BEGIN

SET IDENTITY_INSERT [sec].[UserType] ON

INSERT INTO [sec].[UserType] ([Id], [Name]) VALUES 
   (1, N'Regular')
;

SET IDENTITY_INSERT [sec].[UserType] OFF

END
