IF NOT EXISTS (SELECT 1 FROM [sec].[UserAddressType] WITH (UPDLOCK, HOLDLOCK))
BEGIN

SET IDENTITY_INSERT [sec].[UserAddressType] ON

INSERT INTO [sec].[UserAddressType] ([Id], [Name]) VALUES
  (1, N'Основний'),
  (2, N'Домашній'),
  (3, N'Робочий')
;
--WHEN MATCHED               THEN UPDATE SET t.[Name] = s.[Name];

SET IDENTITY_INSERT [sec].[UserAddressType] OFF

END
