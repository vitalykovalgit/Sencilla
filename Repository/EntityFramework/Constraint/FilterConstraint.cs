#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Sencilla.Repository.EntityFramework;

public class FilterConstraintHandler<TEntity> : IEventHandler<EntityReadingEvent<TEntity>>
    where TEntity : class
{
    public async Task HandleAsync(EntityReadingEvent<TEntity> @event)
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

        if (filter.OrderBy?.Length > 0)
            query = (filter.Descending ?? false)
                  ? query.OrderByDescending(e => EF.Property<object>(e, filter.OrderBy.First()))
                  : query.OrderBy(e => EF.Property<object>(e, filter.OrderBy.First()));

        if (filter.Properties?.Count > 0)
            foreach (var kvp in filter.Properties)
                query = query.Where(ToExpression(kvp.Value));

        if (filter.Skip != null)
            query = query.Skip(filter.Skip.Value);

        if (filter.Take != null)
            query = query.Take(filter.Take.Value);

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

    private static string? ToIncludePath(Type entityType, string with)
    {
        var properties = new List<string>();

        foreach (var w in with.Split('.'))
        {
            var property = entityType?.GetProperty(w, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property != null)
            {
                properties.Add(property.Name);
                entityType = property?.PropertyType;
            }
        }

        return string.Join(".", properties);
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

        if (prop.Type == typeof(Guid)) 
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

            if (prop.Type == typeof(string) || prop.Type == typeof(Guid)) 
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