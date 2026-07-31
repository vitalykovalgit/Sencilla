namespace Sencilla.Component.Tags.Tests;

/// <summary>
/// One entity per storage strategy, plus the three authoring mistakes the registrator must reject. All three
/// storage entities are IEntityUpdateable: a tag write touches its parent through the update pipeline, so an
/// entity with no update repository can carry tags but never records that they changed.
/// </summary>
public class InlineThing : IEntity<int>, IEntityUpdateable, IEntityTaggableInline
{
    public int Id { get; set; }
    public List<string>? Tags { get; set; }
}

public class LinkedThing : IEntity<Guid>, IEntityUpdateable, IEntityTaggableLinked
{
    public Guid Id { get; set; }
    public List<string>? Tags { get; set; }
}

public class LinkedThingTag : EntityTagLink<LinkedThing, Guid>;

public class SharedThing : IEntity<int>, IEntityUpdateable, IEntityTaggableShared
{
    public int Id { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>Taggable but declares no storage — must fail at startup, not at first read.</summary>
public class StrategylessThing : IEntity<int>, IEntityTaggable
{
    public int Id { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>Declares two storages — equally undecidable.</summary>
public class TwoStrategyThing : IEntity<int>, IEntityTaggableInline, IEntityTaggableShared
{
    public int Id { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>Linked, but ships no link entity — the adoption step that is easy to forget.</summary>
public class LinklessThing : IEntity<int>, IEntityTaggableLinked
{
    public int Id { get; set; }
    public List<string>? Tags { get; set; }
}
