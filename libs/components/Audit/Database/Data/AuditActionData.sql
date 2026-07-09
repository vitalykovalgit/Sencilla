-- Seeds audit.AuditAction (ids owned by the C# AuditAction enum). Idempotent: seeds only when empty.
-- ponytail: block-level guard mirrors the sibling lookups; a NEW enum value needs a data migration, not this.
IF NOT EXISTS (SELECT 1 FROM [audit].[AuditAction] WITH (UPDLOCK, HOLDLOCK))
BEGIN

INSERT INTO [audit].[AuditAction] ([Id], [Name]) VALUES
  (1, N'Insert'),
  (2, N'Update'),
  (3, N'Delete')
;

END
