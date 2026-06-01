IF NOT EXISTS (SELECT 1 FROM [sec].[UserStatus] WITH (UPDLOCK, HOLDLOCK))
BEGIN

SET IDENTITY_INSERT [sec].[UserStatus] ON

INSERT INTO [sec].[UserStatus] ([Id], [Name]) VALUES
  (1, N'NotRegistered'),
  (2, N'Registered'),
  (3, N'Completed')
;

SET IDENTITY_INSERT [sec].[UserStatus] OFF

END
