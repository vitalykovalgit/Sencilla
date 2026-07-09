-- Lookup for audit.Audit.[Action]. Ids are owned by the C# AuditAction enum (closed set,
-- never store-generated) so no IDENTITY. Seeded from Data/AuditActionData.sql.
CREATE TABLE [audit].[AuditAction]
(
    [Id]   TINYINT       NOT NULL,          -- = AuditAction enum value (1 Insert, 2 Update, 3 Delete)
    [Name] NVARCHAR(255) NOT NULL,

    CONSTRAINT [PK_AuditAction]      PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UC_AuditAction_Name] UNIQUE ([Name]),
)
