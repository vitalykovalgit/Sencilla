-- One-time migration of [sec].[Matrix].[Action] from sequential action ids
-- (Read=1, Create=2, Update=3, Delete=4, All=5) to bit flags
-- (Read=1, Create=2, Update=4, Delete=8, All=15).
--
-- Must run BEFORE ActionData.sql within ApplyData.sql: the old-world marker is
-- [sec].[Action] row Id=3 still named 'Update' (the flags seed names it
-- 'Read, Create'), so the re-seed would erase the marker. Fresh databases have
-- an empty [sec].[Action] at this point and skip the block entirely.
IF EXISTS (SELECT 1 FROM [sec].[Action] WHERE [Id] = 3 AND [Name] = N'Update')
BEGIN
    -- Target ids must exist before Matrix rows can point at them (FK_Matrix_Action).
    -- ActionData.sql normalizes all names right after.
    SET IDENTITY_INSERT [sec].[Action] ON

    INSERT INTO [sec].[Action] ([Id], [Name])
    SELECT v.[Id], v.[Name]
    FROM (VALUES (8, N'Delete'), (15, N'All')) AS v([Id], [Name])
    WHERE NOT EXISTS (SELECT 1 FROM [sec].[Action] a WHERE a.[Id] = v.[Id])

    SET IDENTITY_INSERT [sec].[Action] OFF

    -- Single pass so remapped values are never remapped again (4->8 must not
    -- catch rows that were just moved 3->4).
    UPDATE [sec].[Matrix]
    SET [Action] = CASE [Action]
                       WHEN 3 THEN 4   -- Update
                       WHEN 4 THEN 8   -- Delete
                       WHEN 5 THEN 15  -- All
                       ELSE [Action]   -- Read=1, Create=2 keep their values
                   END

    -- Erase the old-world marker so this block never runs twice.
    UPDATE [sec].[Action] SET [Name] = N'Read, Create' WHERE [Id] = 3
END
