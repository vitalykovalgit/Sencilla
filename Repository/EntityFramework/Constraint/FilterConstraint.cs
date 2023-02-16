#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Sencilla.Repository.EntityFramework;

public class FilterConstraintHandler<TEntity> : IEventHandler<EntityReadingEvent<TEntity>>
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

        if (filter.Skip != null)
            query = query.Skip(filter.Skip.Value);

        if (filter.Take != null)
            query = query.Take(filter.Take.Value);

        if (filter.OrderBy?.Length > 0)
            query = (filter.Descending ?? false)
                  ? query.OrderByDescending(e => EF.Property<object>(e, filter.OrderBy.First()))
                  : query.OrderBy(e => EF.Property<object>(e, filter.OrderBy.First()));

        if (filter.Properties?.Count > 0)
            foreach (var kvp in filter.Properties)
                query = query.Where(ToExpression(kvp.Value));

        @event.Entities = query;
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

        var vals = new StringBuilder();
        foreach (var v in prop.Values)
        {
            if (prop.Type == typeof(string))
            {
                vals.Append($"\"{v}\",");
            }
            else
            {
                vals.Append($"{v},");
            }
        }

        if (vals.Length > 0)
            vals.Remove(vals.Length - 1, 1);

        return $"{prop.Query} in ({vals})";
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
