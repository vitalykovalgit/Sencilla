using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// The component's half of the model seam: what <c>DynamicDbContext</c> does with a taggable entity is decided
/// HERE, not there. Inline storage bounds the column (or EF sends <c>nvarchar(-1)</c> parameters at the
/// <c>NVARCHAR(4000)</c> the dacpac declares); side-table storage removes it, because EF's conventions would
/// otherwise map the wire-contract property as a phantom column no repository writes.
/// </summary>
public class TagsModelConfiguratorTests
{
    [Fact]
    public void InlineEntity_GetsTheColumnBounded()
    {
        var model = Configure<InlineThing>();

        Assert.Equal(TagName.MaxSetLength, model.FindEntityType(typeof(InlineThing))!.FindProperty(nameof(IEntityTaggable.Tags))!.GetMaxLength());
    }

    [Fact]
    public void LinkedEntity_HasTheColumnIgnored()
    {
        // Asserted as "ignored", not "absent": EF's primitive-collection convention runs at model finalisation,
        // so an unmapped property is absent here for uninteresting reasons too.
        Assert.Contains(nameof(IEntityTaggable.Tags), Ignored<LinkedThing>());
    }

    [Fact]
    public void SharedEntity_HasTheColumnIgnored()
    {
        Assert.Contains(nameof(IEntityTaggable.Tags), Ignored<SharedThing>());
    }

    [Fact]
    public void NonTaggableEntity_IsLeftAlone()
    {
        // Configure() is called for EVERY mapped entity in the application, so "does nothing" is a contract —
        // a same-named property on an entity that never opted in must survive untouched.
        Assert.DoesNotContain(nameof(PlainThing.Tags), Ignored<PlainThing>());
    }

    static IEnumerable<string> Ignored<TEntity>() where TEntity : class
        => Configure<TEntity>().FindEntityType(typeof(TEntity))!.GetIgnoredMembers();

    static IMutableModel Configure<TEntity>() where TEntity : class
    {
        var builder = new ModelBuilder();

        new TagsModelConfigurator().Configure(builder.Entity(typeof(TEntity)), typeof(TEntity));

        return builder.Model;
    }

    /// <summary>Not taggable, but carries a same-named property — nothing may touch it.</summary>
    class PlainThing : IEntity<int>
    {
        public int Id { get; set; }
        public List<string>? Tags { get; set; }
    }
}
