namespace Sencilla.Web;

[DisableInjection]
public class FilterTypeBinder : IModelBinder
{
    ComplexTypeModelBinder ComplexTypeModelBinder;

    IDictionary<ModelMetadata, IModelBinder> FilterProperties;
    IDictionary<ModelMetadata, IModelBinder> EntityProperties;
    IDictionary<string, ModelMetadata> EntityArrayProperties;

    public FilterTypeBinder(
        ILoggerFactory logger,
        IDictionary<ModelMetadata, IModelBinder> filterProperties,
        IDictionary<ModelMetadata, IModelBinder> entityProperties,
        IDictionary<string, ModelMetadata> entityArrayProperties)
    {
        FilterProperties = filterProperties;
        EntityProperties = entityProperties;
        EntityArrayProperties = entityArrayProperties;
        ComplexTypeModelBinder = new ComplexTypeModelBinder(filterProperties, logger);
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        // Bind values passed by user to filter
        await ComplexTypeModelBinder.BindModelAsync(bindingContext);

        var parameters = bindingContext.HttpContext.Request.Query;
        foreach (var propName in parameters.Keys)
        {
            // If they defined in filter skip it, so properties dictionary will contains only 
            // properties not defined in filter 
            if (FilterProperties.Any(p => p.Key.Name.Equals(propName, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Check if properties defined in entity 
            // Use only properties that are defined in entity 
            var property = EntityProperties.FirstOrDefault(p => p.Key.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
            if (!property.Equals(default(KeyValuePair<ModelMetadata, IModelBinder>)))
            {
                // Get values as array for the property
                ModelBindingResult result;
                var propertyType = property.Key.ModelType;//Nullable.GetUnderlyingType(property.Key.ModelType) ?? property.Key.ModelType;

                // Only scalar properties can be filtered from a query string. A navigation or collection
                // property (a nested entity, a List<T> — e.g. a tag link collection) binds to nothing, and the
                // loop below would then dereference a null model: one mistyped query param, one 500. Such a
                // property is not filterable at all, so skip it rather than half-bind it.
                if (!IsFilterable(propertyType) || !EntityArrayProperties.TryGetValue(propertyType.FullName ?? "", out var metaArray))
                    continue;

                using (bindingContext.EnterNestedScope(metaArray, bindingContext.FieldName, propName, null))
                {
                    await property.Value.BindModelAsync(bindingContext);
                    result = bindingContext.Result;
                }

                // Check if model is a filter and add to list of properties
                var filter = bindingContext.Model as IFilter;
                if (filter != null && result.Model is System.Collections.IEnumerable values)
                {
                    foreach (var v in values)
                        filter.AddProperty(propName, propertyType, v);
                }
            }
            else
            {
                // think how secure it is
                var filter = bindingContext.Model as IFilter;
                filter?.AddProperty(propName, null);
            }
        }
    }

    /// <summary>
    /// Types a query-string value can actually be bound to and compared against — an allowlist, because the
    /// alternative (anything not obviously complex) lets navigation properties, dictionaries and JSON-valued
    /// columns through to the filter, where they are at best meaningless and at worst a 500.
    /// </summary>
    static bool IsFilterable(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        return t.IsPrimitive
            || t.IsEnum
            || t == typeof(string)
            || t == typeof(Guid)
            || t == typeof(decimal)
            || t == typeof(DateTime)
            || t == typeof(DateTimeOffset)
            || t == typeof(DateOnly)
            || t == typeof(TimeOnly)
            || t == typeof(TimeSpan);
    }
}
