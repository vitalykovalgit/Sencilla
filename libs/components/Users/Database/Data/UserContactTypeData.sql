IF NOT EXISTS (SELECT 1 FROM [sec].[UserContactType] WITH (UPDLOCK, HOLDLOCK))
BEGIN

SET IDENTITY_INSERT [sec].[UserContactType] ON

INSERT INTO [sec].[UserContactType] ([Id], [Hidden], [Order], [Name], [Icon]) VALUES (1, 0, 0, N'Viber', 'viber'),
  (2, 1, 1, N'Messenger', 'messanger'),
  (3, 1, 2, N'WatsUp', 'watsup'),
  (4, 0, 3, N'Telegram', 'telegram'),
  (5, 1, 4, N'Signal', 'signal'),
  (6, 1, 5, N'Skype', 'skype'),
  (7, 1, 6, N'Zoom', 'zoom'),
  (8, 1, 7, N'Teams', 'teams'),
  (9, 0, 8, N'Facebook', 'facebook'),
  (10, 0, 8, N'Instagram', 'instagram')
;

SET IDENTITY_INSERT [sec].[UserContactType] OFF

END
