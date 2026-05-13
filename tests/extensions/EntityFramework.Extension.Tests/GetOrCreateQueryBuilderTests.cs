namespace Sencilla.EntityFramework.Extension.Tests;

public class GetOrCreateQueryBuilderTests
{
    private readonly TestEntity _te;
    private readonly TestEntity _te2;

    public GetOrCreateQueryBuilderTests()
    {
        _te = new TestEntity
        {
            Id = Guid.NewGuid(),
            Phone = 8098,
            Email = "test@gmail.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            CreatedDate = new DateTime(2026, 1, 1),
            UpdatedDate = new DateTime(2026, 1, 2),
        };

        _te2 = new TestEntity
        {
            Id = Guid.NewGuid(),
            Phone = 9999,
            Email = "other@gmail.com",
            FirstName = "Jane",
            LastName = "Smith",
            IsActive = false,
            CreatedDate = new DateTime(2026, 2, 1),
            UpdatedDate = new DateTime(2026, 2, 2),
        };
    }

    // ── Default key (Id) ──────────────────────────────────────────────────────

    [Fact]
    public void Build_DefaultKey_UsesIdInOnClause()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>([]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("t.[Id] = s.[Id]", merge);
    }

    [Fact]
    public void Build_DefaultKey_OutputContainsId()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>([]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("OUTPUT INSERTED.[Id]", merge);
    }

    [Fact]
    public void Build_DefaultKey_NoUpdateClause()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>([]);
        var (merge, _) = builder.Build([_te]);

        Assert.DoesNotContain("WHEN MATCHED", merge);
    }

    [Fact]
    public void Build_DefaultKey_NoDeleteClause()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>([]);
        var (merge, _) = builder.Build([_te]);

        Assert.DoesNotContain("NOT MATCHED BY SOURCE", merge);
    }

    // ── String key selector ───────────────────────────────────────────────────

    [Fact]
    public void Build_SingleStringKey_UsesKeyInOnClause()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Email"]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("t.[Email] = s.[Email]", merge);
    }

    [Fact]
    public void Build_SingleStringKey_OutputContainsKey()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Email"]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("OUTPUT INSERTED.[Email]", merge);
    }

    [Fact]
    public void Build_MultipleStringKeys_AllKeysInOnClause()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Email", "Phone"]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("t.[Email] = s.[Email]", merge);
        Assert.Contains("t.[Phone] = s.[Phone]", merge);
        Assert.Contains("AND", merge);
    }

    [Fact]
    public void Build_MultipleStringKeys_AllKeysInOutputClause()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Email", "Phone"]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("INSERTED.[Email]", merge);
        Assert.Contains("INSERTED.[Phone]", merge);
    }

    // ── Case-insensitive key resolution ───────────────────────────────────────

    [Fact]
    public void Build_LowercaseKey_ResolvesPropertyCaseInsensitive()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["email"]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("t.[Email] = s.[Email]", merge);
    }

    [Fact]
    public void Build_MixedCaseKey_ResolvesPropertyCaseInsensitive()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["FIRSTNAME"]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("t.[FirstName] = s.[FirstName]", merge);
    }

    // ── SELECT part ───────────────────────────────────────────────────────────

    [Fact]
    public void Build_SelectPart_ContainsInnerJoinByKey()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Email"]);
        var (_, select) = builder.Build([_te]);

        Assert.Contains("SELECT t.*", select);
        Assert.Contains("INNER JOIN", select);
        Assert.Contains("t.[Email] = s.[Email]", select);
    }

    [Fact]
    public void Build_SelectPart_ContainsEntityKeyValue()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Email"]);
        var (_, select) = builder.Build([_te]);

        Assert.Contains(_te.Email, select);
    }

    // ── Multiple entities ─────────────────────────────────────────────────────

    [Fact]
    public void Build_MultipleEntities_AllValuesInMerge()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Email"]);
        var (merge, _) = builder.Build([_te, _te2]);

        Assert.Contains(_te.Email, merge);
        Assert.Contains(_te2.Email, merge);
    }

    [Fact]
    public void Build_MultipleEntities_AllKeyValuesInSelect()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Email"]);
        var (_, select) = builder.Build([_te, _te2]);

        Assert.Contains(_te.Email, select);
        Assert.Contains(_te2.Email, select);
    }

    // ── Target table name ─────────────────────────────────────────────────────

    [Fact]
    public void Build_UsesTableAttributeNameAndSchema()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>([]);
        var (merge, select) = builder.Build([_te]);

        Assert.Contains("[test].[TestEntity]", merge);
        Assert.Contains("[test].[TestEntity]", select);
    }

    // ── SQL injection escaping ────────────────────────────────────────────────

    [Fact]
    public void Build_SqlInjectionInStringField_EscapesSingleQuote()
    {
        _te.Email = "hack@x.com'; DROP TABLE Users;--";
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Email"]);

        // Must not throw; the single quote must be escaped so it cannot break
        // out of the SQL string literal (the text is safe data, not code).
        var (merge, _) = builder.Build([_te]);

        Assert.NotEmpty(merge);
        Assert.Contains(@"\'", merge);
    }

    // ── Null field values ─────────────────────────────────────────────────────

    [Fact]
    public void Build_NullStringField_EmitsNullInSql()
    {
        _te.Email = null;
        var builder = new GetOrCreateQueryBuilder<TestEntity>(["Phone"]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("NULL", merge);
    }

    // ── Only insert, no update ────────────────────────────────────────────────

    [Fact]
    public void Build_MergePart_ContainsWhenNotMatchedInsert()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntity>([]);
        var (merge, _) = builder.Build([_te]);

        Assert.Contains("WHEN NOT MATCHED BY TARGET THEN", merge);
        Assert.Contains("INSERT", merge);
    }

    // ── Navigation property exclusion ─────────────────────────────────────────

    [Fact]
    public void GetMappedProperties_ExcludesReferenceNavigationProperty()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntityWithNavProps>([]);
        var props = builder.GetMappedProperties();

        Assert.DoesNotContain(props, p => p.Name == nameof(TestEntityWithNavProps.Child));
    }

    [Fact]
    public void GetMappedProperties_ExcludesCollectionNavigationProperty()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntityWithNavProps>([]);
        var props = builder.GetMappedProperties();

        Assert.DoesNotContain(props, p => p.Name == nameof(TestEntityWithNavProps.Children));
    }

    [Fact]
    public void GetMappedProperties_ExcludesNotMappedProperty()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntityWithNavProps>([]);
        var props = builder.GetMappedProperties();

        Assert.DoesNotContain(props, p => p.Name == nameof(TestEntityWithNavProps.Computed));
    }

    [Fact]
    public void GetMappedProperties_IncludesScalarProperties()
    {
        var builder = new GetOrCreateQueryBuilder<TestEntityWithNavProps>([]);
        var props = builder.GetMappedProperties();

        Assert.Contains(props, p => p.Name == nameof(TestEntityWithNavProps.Id));
        Assert.Contains(props, p => p.Name == nameof(TestEntityWithNavProps.Name));
        Assert.Contains(props, p => p.Name == nameof(TestEntityWithNavProps.ChildId));
    }

    [Fact]
    public void Build_WithNavigationProperties_DoesNotIncludeNavColumnInMerge()
    {
        var entity = new TestEntityWithNavProps
        {
            Id = 1,
            Name = "test",
            ChildId = 42,
            Child = new TestChildEntity { Id = 42, Label = "child" },
            Children = [new TestChildEntity { Id = 43, Label = "other" }],
        };
        var builder = new GetOrCreateQueryBuilder<TestEntityWithNavProps>(["Name"]);
        var (merge, _) = builder.Build([entity]);

        Assert.DoesNotContain("[Child]", merge);
        Assert.DoesNotContain("[Children]", merge);
        Assert.Contains("[Name]", merge);
    }
}
