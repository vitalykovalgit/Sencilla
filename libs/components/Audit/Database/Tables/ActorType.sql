-- Lookup for audit.Audit.[ActorType]. Ids are owned by the C# ActorType enum (closed set,
-- never store-generated) so no IDENTITY — and it includes 0 (System), which IDENTITY can't seed.
-- Seeded from Data/ActorTypeData.sql.
CREATE TABLE [audit].[ActorType]
(
    [Id]   TINYINT       NOT NULL,          -- = ActorType enum value (0 System, 1 User, 2 Admin)
    [Name] NVARCHAR(255) NOT NULL,

    CONSTRAINT [PK_ActorType]      PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UC_ActorType_Name] UNIQUE ([Name]),
)
