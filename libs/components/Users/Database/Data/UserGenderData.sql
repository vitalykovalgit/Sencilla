IF NOT EXISTS (SELECT 1 FROM [sec].[UserGender] WITH (UPDLOCK, HOLDLOCK))
BEGIN

SET IDENTITY_INSERT [sec].[UserGender] ON

INSERT INTO [sec].[UserGender] ([Id], [Name]) VALUES
  (1, N'Чоловік'),
  (2, N'Жінка')
;

SET IDENTITY_INSERT [sec].[UserGender] OFF

END
