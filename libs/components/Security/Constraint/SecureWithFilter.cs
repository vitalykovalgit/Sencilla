namespace Sencilla.Component.Security;

/// <summary>
/// Composes the "caller holds role R on this row" predicate for a matrix row whose
/// role the caller does not hold globally. The predicate is a plain expression tree
/// over the entity's [SecureWith] chain, so it composes into the query exactly like
/// a parsed constraint — permissions stay compiled INTO the SQL, and filtered lists
/// remain free.
///
/// Generated shapes (linkQ / parentQ are raw repository queries — no entity events,
/// no recursion into security checks):
/// <code>
/// // Project (terminal):
/// e => linkQ.Any(l => l.UserId == @me &amp;&amp; grantors.Contains(l.RoleId) &amp;&amp; l.EntityId == e.Id)
///
/// // ProjectItem (1 hop — the FK itself carries the terminal key, no join):
/// e => linkQ.Any(l => l.UserId == @me &amp;&amp; grantors.Contains(l.RoleId) &amp;&amp; l.EntityId == e.ProjectId)
///
/// // ProjectFile (2 hops — each intermediate hop adds one parent EXISTS):
/// e => linkQ.Any(l => l.UserId == @me &amp;&amp; grantors.Contains(l.RoleId)
///        &amp;&amp; itemQ.Any(i => i.Id == e.ItemId &amp;&amp; l.EntityId == i.ProjectId))
/// </code>
/// `grantors` is the reverse role closure — holding Owner activates an Editor row
/// because Owner inherits Editor's grants (<see cref="IRoleClosure.GrantorsOf"/>).
///
/// All queryables must root in the SAME scoped DbContext as the outer query
/// (standard scoped-repository setup) — EF only translates correlated subqueries
/// within one context. Role-table/parent properties must be public (explicit
/// interface implementations are not supported).
/// </summary>
public static class SecureWithFilter
{
    /// <summary>
    /// The held-role predicate for <typeparamref name="TEntity"/> and a matrix row's
    /// role, or null when the entity declares no [SecureWith] chain (the row is then
    /// activatable by global holding only).
    ///
    /// <paramref name="excludeSelfKeys"/> — set ONLY on the create post-image of a
    /// self-securing role table (the entity IS its own link table, e.g.
    /// <c>[SecureWith(typeof(Project), Via = nameof(ProjectUser.EntityId))]</c> on
    /// ProjectUser). Without it the just-inserted role row would satisfy its OWN
    /// authorization ("I hold Owner because I just wrote that I do") — an escalation.
    /// The excluded keys make the EXISTS require a PRE-existing holding row instead.
    /// </summary>
    public static Expression<Func<TEntity, bool>>? Build<TEntity>(int matrixRoleId, Guid userId, IServiceProvider services, IRoleClosure closure, IReadOnlyCollection<object>? excludeSelfKeys = null)
    {
        var chain = SecureWithChains.TryResolve(typeof(TEntity));
        if (chain == null)
            return null;

        // Roles whose holders activate this row: the row's role plus every role that
        // inherits its grants (Owner ⊃ Editor ⊃ Viewer via Role.Parent).
        var grantors = closure.GrantorsOf(matrixRoleId).ToList();

        var entity = Expression.Parameter(typeof(TEntity), "e");
        var link = Expression.Parameter(chain.LinkType, "l");

        var body = Expression.AndAlso(
            Expression.Equal(Prop(link, "UserId"), Expression.Constant(userId)),
            Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(int)],
                Expression.Constant(grantors), Prop(link, "RoleId")));

        body = Expression.AndAlso(body, BuildReach(entity, link, chain, services));

        // Self-securing role table on create: the holding must come from a row OTHER
        // than the ones inserted in this operation (which are the rows being authorized).
        if (excludeSelfKeys is { Count: > 0 } && chain.LinkType == typeof(TEntity))
            body = Expression.AndAlso(body, Expression.Not(ContainsKey(link, chain.LinkKeyType, excludeSelfKeys)));

        var linkPredicate = Expression.Lambda(body, link);
        var any = Expression.Call(typeof(Queryable), nameof(Queryable.Any), [chain.LinkType],
            QueryOf(services, chain.LinkType, chain.LinkKeyType), Expression.Quote(linkPredicate));

        return Expression.Lambda<Func<TEntity, bool>>(any, entity);
    }

    /// <summary><c>keys.Contains(link.Id)</c> with the key list typed as List&lt;TKey&gt; so EF can translate it.</summary>
    private static Expression ContainsKey(ParameterExpression link, Type keyType, IReadOnlyCollection<object> keys)
    {
        var typed = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(keyType))!;
        foreach (var k in keys)
            typed.Add(k);

        return Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [keyType],
            Expression.Constant(typed, typeof(List<>).MakeGenericType(keyType)), Prop(link, "Id"));
    }

    /// <summary>
    /// The correlation between the queried row and the role row: walks the FK hops
    /// from the entity to the terminal key. The LAST hop's Via already carries the
    /// terminal key value, so only intermediate hops need a parent EXISTS join.
    /// </summary>
    private static Expression BuildReach(ParameterExpression entity, ParameterExpression link, SecureWithChain chain, IServiceProvider services)
    {
        var linkEntityId = Prop(link, "EntityId");

        // Terminal entity queried directly: correlate on its own key.
        if (chain.Hops.Count == 0)
            return EqualLifted(linkEntityId, Prop(entity, "Id"));

        return Join(0, Prop(entity, chain.Hops[0].Via.Name));

        // keyExpr yields the key of hop level i's SOURCE (the entity hop i points at).
        Expression Join(int i, Expression keyExpr)
        {
            if (i == chain.Hops.Count - 1)
                return EqualLifted(linkEntityId, keyExpr);

            var (parentType, parentVia) = chain.Hops[i + 1];
            var parent = Expression.Parameter(parentType, $"p{i}");
            var body = Expression.AndAlso(
                EqualLifted(Prop(parent, "Id"), keyExpr),
                Join(i + 1, Prop(parent, parentVia.Name)));

            var parentKey = SecureWithChains.KeyOf(parentType)!;
            return Expression.Call(typeof(Queryable), nameof(Queryable.Any), [parentType],
                QueryOf(services, parentType, parentKey), Expression.Quote(Expression.Lambda(body, parent)));
        }
    }

    /// <summary>
    /// Raw repository query of an entity as an expression node — bypasses the entity
    /// pipeline deliberately (framework-internal read; going through events here would
    /// recurse into the security checks being evaluated).
    ///
    /// Returns the queryable's own <see cref="IQueryable.Expression"/> (for EF an
    /// EntityQueryRootExpression) — NOT Expression.Constant(query). EF Core's query
    /// preprocessor only recognises a real query root; a bare ConstantExpression
    /// wrapping the DbSet is left unexpanded and translation throws
    /// "could not be translated" at runtime.
    /// </summary>
    private static Expression QueryOf(IServiceProvider services, Type entityType, Type keyType)
    {
        var repoType = typeof(IReadRepository<,>).MakeGenericType(entityType, keyType);
        var repo = services.GetRequiredService(repoType);
        var query = (IQueryable)repoType.GetProperty(nameof(IReadRepository<IEntity<int>, int>.Query))!.GetValue(repo)!;

        var root = query.Expression;
        var wanted = typeof(IQueryable<>).MakeGenericType(entityType);
        return root.Type == wanted ? root : Expression.Convert(root, wanted);
    }

    private static MemberExpression Prop(Expression target, string name) =>
        Expression.Property(target, target.Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{target.Type.Name} has no public property '{name}' (explicit interface implementations are not supported by [SecureWith])."));

    /// <summary>Equality tolerant of a nullable FK against a non-nullable key (Guid? vs Guid).</summary>
    private static BinaryExpression EqualLifted(Expression a, Expression b)
    {
        if (a.Type != b.Type)
        {
            if (Nullable.GetUnderlyingType(a.Type) == b.Type)
                b = Expression.Convert(b, a.Type);
            else if (Nullable.GetUnderlyingType(b.Type) == a.Type)
                a = Expression.Convert(a, b.Type);
        }

        return Expression.Equal(a, b);
    }
}
