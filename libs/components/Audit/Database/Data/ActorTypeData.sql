-- Seeds audit.ActorType (ids owned by the C# ActorType enum). Idempotent: seeds only when empty.
-- ponytail: block-level guard mirrors the sibling lookups; a NEW enum value needs a data migration, not this.
IF NOT EXISTS (SELECT 1 FROM [audit].[ActorType] WITH (UPDLOCK, HOLDLOCK))
BEGIN

INSERT INTO [audit].[ActorType] ([Id], [Name]) VALUES
  (0, N'System'),
  (1, N'User'),
  (2, N'Admin')
;

END
