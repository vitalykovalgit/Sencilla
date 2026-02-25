namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="FileFilter"/>.
///
/// Covers: ByParentId, ByDimmension, fluent chaining.
/// </summary>
public class FileFilterTests
{
    [Fact]
    public void ByParentId_SingleId_AddsPropertyToFilter()
    {
        var filter = new FileFilter();
        var parentId = Guid.NewGuid();

        var result = filter.ByParentId(parentId);

        Assert.Same(filter, result);
        Assert.Single(filter.Properties);
    }

    [Fact]
    public void ByParentId_MultipleIds_AddsAllPropertiesToFilter()
    {
        var filter = new FileFilter();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        filter.ByParentId(id1, id2, id3);

        Assert.Single(filter.Properties);
        var values = filter.GetProperty(nameof(Sencilla.Component.Files.File.ParentId))?.ToList();
        Assert.NotNull(values);
        Assert.Equal(3, values!.Count);
    }

    [Fact]
    public void ByDimmension_SetsFilterProperty()
    {
        var filter = new FileFilter();

        var result = filter.ByDimmension(100);

        Assert.Same(filter, result);
        Assert.Single(filter.Properties);
    }

    [Fact]
    public void ByDimmension_NullValue_StillAddsProperty()
    {
        var filter = new FileFilter();

        filter.ByDimmension(null);

        Assert.Single(filter.Properties);
    }

    [Fact]
    public void FluentChaining_ByParentIdAndDimmension_AddsBothProperties()
    {
        var parentId = Guid.NewGuid();

        var filter = new FileFilter()
            .ByParentId(parentId)
            .ByDimmension(200);

        Assert.Equal(2, filter.Properties.Count);
    }

    [Fact]
    public void ById_SingleId_AddsPropertyToFilter()
    {
        var filter = new FileFilter();
        var id = Guid.NewGuid();

        var result = filter.ById(id);

        Assert.Same(filter, result);
        Assert.Single(filter.Properties);
    }

    [Fact]
    public void ById_MultipleIds_AddsAllValues()
    {
        var filter = new FileFilter();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        filter.ById(id1, id2);

        Assert.Single(filter.Properties);
        var values = filter.GetProperty(nameof(File.Id))?.ToList();
        Assert.NotNull(values);
        Assert.Equal(2, values!.Count);
    }

    [Fact]
    public void FluentChaining_ByIdAndByParentIdAndByDimmension_AddsAllProperties()
    {
        var filter = new FileFilter()
            .ById(Guid.NewGuid())
            .ByParentId(Guid.NewGuid())
            .ByDimmension(300);

        Assert.Equal(3, filter.Properties.Count);
    }
}
