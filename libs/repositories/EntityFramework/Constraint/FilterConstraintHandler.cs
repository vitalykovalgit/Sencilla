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
    /// Renders one criterion as a Dynamic LINQ predicate string.
    ///
    /// <para>No type at all means the caller supplied a raw expression — pass it through. Otherwise the
    /// property name is compared against the values, with a null among them meaning "or unset".</para>
    /// </summary>
    public static string? ToExpression(FilterProperty prop)
    {
        if (prop.Type == null)
            return prop.Query;

        if (prop.Values == null || prop.Values.Count == 0)
            return prop.Query;

        var type = Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;

        // Null is split out before anything else: it cannot go inside `in (...)`, and a list that held
        // nothing else would render `X in ()` — "Expression expected", i.e. a 500 from a query string.
        // Mixed values used to drop the null silently, which contradicted a lone null already meaning
        // "is null".
        var values = prop.Values.Where(v => v is not null).ToList();
        var nullExp = values.Count == prop.Values.Count ? null : $"{prop.Query} == null";

        if (values.Count == 0)
            return nullExp ?? prop.Query;

        var exp = ToComparison(prop.Query, type, values);

        return nullExp == null ? exp : $"({exp}) || {nullExp}";
    }

    /// <summary>
    /// Guid and bool must NOT go through <c>in (...)</c>, for two unrelated reasons — both verified
    /// against System.Linq.Dynamic.Core 1.7.1 in FilterGuidExpressionTests / FilterBoolExpressionTests:
    ///
    /// <para>· Guid — <c>ParseIn</c> calls GenerateEqual on the raw operands and skips the string→Guid
    /// promotion that <c>ParseComparisonOperator</c> performs, so <c>Id in ("…")</c> throws "Operator
    /// '==' incompatible with operand types 'Guid' and 'String'" while the very same quoted literal
    /// compares fine as <c>Id == "…"</c>. It fails on a single value too, and single quotes are not an
    /// escape hatch — <c>'…'</c> is a CHAR literal there.</para>
    ///
    /// <para>· bool — <c>ParseIn</c> seeds its OR accumulator with the LEFT operand and then asks
    /// <c>accumulate.Type != typeof(bool)</c> to tell "nothing accumulated yet" apart from "keep
    /// OR-ing". For a NON-nullable bool column that seed is already a bool expression, so the column
    /// itself is ORed into the chain: <c>test in (False)</c> compiles to <c>Test || Test == false</c>
    /// — a tautology that silently returns EVERY row, exactly the shape a dropped criterion has.
    /// <c>bool?</c> slips past the check, which is why a nullable flag column filters correctly while
    /// the non-nullable one beside it does not. <c>in (True)</c> looked fine only by accident:
    /// <c>Test || Test == true</c> is still <c>Test == true</c>.</para>
    ///
    /// An explicit <c>==</c> chain is correct for both, and for bool it is also what the values already
    /// render as — <c>$"{true}"</c> is "True", which Dynamic LINQ parses case-insensitively.
    /// </summary>
    private static string ToComparison(string? query, Type type, List<object?> values)
    {
        if (type == typeof(Guid) || type == typeof(bool))
        {
            var quote = type == typeof(Guid) ? "\"" : "";
            return string.Join(" || ", values.Select(v => $"{query} == {quote}{v}{quote}"));
        }

        var literals = type == typeof(string)
            ? values.Select(v => $"\"{v}\"")
            : values.Select(v => $"{v}");

        return $"{query} in ({string.Join(",", literals)})";
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
