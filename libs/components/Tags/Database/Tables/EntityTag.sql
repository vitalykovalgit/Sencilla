CREATE TABLE [tag].[EntityTag]
(
    [Id]          BIGINT IDENTITY NOT NULL,

    -- Tagged row, type-agnostic and deliberately FK-less (audit-log style) so any entity can be tagged
    -- without shipping a line of DDL. [Entity] is typeof(TEntity).Name, PascalCase (e.g. 'PriceRule').
    [Entity]      NVARCHAR(64)    NOT NULL,
    [EntityId]    NVARCHAR(64)    NOT NULL,          -- tagged row's id, stringified (Guid or int)

    -- Normalised by Sencilla.Core TagName: lowercased, trimmed, charset [a-z0-9-_.:]. The default CI
    -- collation makes the uniqueness below case-insensitive too, which matches that normalisation.
    [Name]        NVARCHAR(64)    NOT NULL,

    [CreatedDate] DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_EntityTag] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- One row per (row, tag): the store writes diffs, and a duplicate would double-count in every read.
    CONSTRAINT [UX_EntityTag_Row] UNIQUE NONCLUSTERED ([Entity] ASC, [EntityId] ASC, [Name] ASC),

    -- "Which rows carry this tag" — the ?tag= filter's only lookup path, and the autocomplete listing's.
    INDEX [IX_EntityTag_Name] NONCLUSTERED ([Entity] ASC, [Name] ASC)
)
