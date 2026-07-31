#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Sencilla.Repository.EntityFramework;

public class FilterConstraintHandler<TEntity> : IEventHandler<EntityReadingEvent<TEntity>>
    where TEntity : class
{
    public async Task HandleAsync(EntityReadingEvent<TEntity> @event, CancellationToken token)
    {
        if (@event == null)
            return;

        // check if filter is not null
        var filter = @event.Filter;
        if (filter == null)
            return;

        var query = @event.Entities;
        if (query == null)
            return;

        if (filter.Properties?.Count > 0)
            foreach (var kvp in filter.Properties)
                query = query.Where(ToExpression(kvp.Value));

        if (filter.With?.Length > 0)
        {
            var entityType = typeof(TEntity);

            foreach (var with in filter.With)
            {
                var include = ToIncludePath(entityType, with);

                if (include?.Length > 0)
                    query = query.Include(include);
            }
        }

        @event.Entities = query;
    }

    /// <summary>
    /// Resolves one <c>?with=</c> value to an EF include path, or null when it isn't one.
    ///
    /// <para>All-or-nothing per value: a path whose first segment resolves and whose second does not used to
    /// yield the truncated prefix, silently including something the caller never asked for.</para>
    /// </summary>
    private static string? ToIncludePath(Type entityType, string with)
    {
        var properties = new List<string>();

        foreach (var w in with.Split('.'))
        {
            var property = entityType?.GetProperty(w, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null || !CanBeIncluded(property.PropertyType))
                return null;

            properties.Add(property.Name);
            entityType = Target(property.PropertyType);
        }

        return string.Join(".", properties);
    }

    /// <summary>
    /// Where a navigation leads: the element type for a collection, the property type otherwise. Walking to the
    /// ELEMENT is what lets a nested path through a collection (<c>?with=items.product</c>) resolve at all.
    /// </summary>
    private static Type Target(Type type)
        => type.GetInterfaces()
               .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
               ?.GetGenericArguments()[0] ?? type;

    /// <summary>
    /// Whether a CLR property could be an EF navigation at all. <c>Include</c> accepts nothing else and fails at
    /// query-COMPILE time, not at the <c>Include</c> call — so handing it a scalar turns a <c>?with=</c> typo
    /// into a 500 from a query string. A collection of scalars is not a navigation either, which is what makes
    /// <c>?with=tags</c> (<c>List&lt;string&gt;</c>, whether mapped as a primitive collection or ignored
    /// outright) a no-op here rather than a crash.
    ///
    /// <para>ponytail: CLR shape only. A property that IS entity-shaped but which EF is configured to ignore
    /// still gets through; consult <c>DbContext.Model</c> here if that ever bites.</para>
    /// </summary>
    private static bool CanBeIncluded(Type type)
    {
        var target = Target(type);
        target = Nullable.GetUnderlyingType(target) ?? target;

        return target.IsClass && target != typeof(string);
    }

    /// <summary>
    /// If no values and type we will treat it as a query 
    /// otherwise it will treat query as name of property in entity
    /// and convert it to expr: name in (val1, val2) 
    /// </summary>
    /// <param name="prop"></param>
    /// <returns></returns>
    public static string? ToExpression(FilterProperty prop)
    {
        if (prop.Type == null)
            return prop.Query;

        if (prop.Values == null || prop.Values.Count == 0)
            return prop.Query;

        if (prop.Values.Count == 1 && prop.Values[0] == null)
        {
            return $"{prop.Query} == null";
        }

        var type = Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;

        if (type == typeof(Guid))
        {
            var exp = new StringBuilder();
            foreach (var val in prop.Values) 
            {
                if (exp.Length > 0)
                    exp.Append(" || ");

                exp.Append($"{prop.Query} == \"{val}\"");
            }
            return exp.ToString();
        }

        var vals = new StringBuilder();
        foreach (var v in prop.Values)
        {
            // ignore null for now 
            if (v is null) continue;

            if (type == typeof(string) || type == typeof(Guid))
                vals.Append($"\"{v}\",");
            else
                vals.Append($"{v},");
        }

        if (vals.Length > 0)
            vals.Remove(vals.Length - 1, 1);

        return $"{prop.Query} in ({vals})";
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

/**

    public static string? ToExpression(FilterProperty prop)
    {
        if (prop.Type == null)
            return prop.Query;

        if (prop.Values == null || prop.Values.Count == 0)
            return prop.Query;


        //if (prop.Values.Count == 1 && prop.Values[0] == null)
        //{
        //    return $"{prop.Query} IS NULL";
        //}

        var nullExp = "";
    var vals = new StringBuilder();
        foreach (var v in prop.Values)
        {
            // if value equals null update isNull
            if (v == null)
            {
                nullExp = $"({prop.Query} IS NULL)";
                continue;
            }
            
            if (prop.Type == typeof(string))
    vals.Append($"\"{v}\",");
else
    vals.Append($"{v},");
        }

        // remove last comma 
        if (vals.Length > 0)
    vals.Remove(vals.Length - 1, 1);

// 
var exp = vals.Length > 0 ? $"({prop.Query} in ({vals}))" : "";
exp = exp.Length > 0 ? $"({exp} OR {nullExp})" : nullExp;

if (exp.Length == 0)
    return prop.Query;

return exp;
    }
}

 */