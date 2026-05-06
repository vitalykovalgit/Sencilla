namespace Sencilla.Web;

[DisableInjection]
public class FilterTypeBinder : IModelBinder
{
    ComplexTypeModelBinder ComplexTypeModelBinder;

    IDictionary<ModelMetadata, IModelBinder> FilterProperties;
    IDictionary<ModelMetadata, IModelBinder> EntityProperties;
    IDictionary<string, ModelMetadata> EntityArrayProperties;
    ISet<string> JsonArrayProperties;

    public FilterTypeBinder(
        ILoggerFactory logger,
        IDictionary<ModelMetadata, IModelBinder> filterProperties,
        IDictionary<ModelMetadata, IModelBinder> entityProperties,
        IDictionary<string, ModelMetadata> entityArrayProperties,
        ISet<string>? jsonArrayProperties = null)
    {
        FilterProperties = filterProperties;
        EntityProperties = entityProperties;
        EntityArrayProperties = entityArrayProperties;
        JsonArrayProperties = jsonArrayProperties ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                var propertyType = property.Key.ModelType;

                var metaArray = EntityArrayProperties[propertyType.FullName];
                using (bindingContext.EnterNestedScope(metaArray, bindingContext.FieldName, propName, null))
                {
                    await property.Value.BindModelAsync(bindingContext);
                    result = bindingContext.Result;
                }

                // Check if model is a filter and add to list of properties 
                var filter = bindingContext.Model as IFilter;
                if (filter != null)
                {
                    // DateTime range.
                    var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                    if (underlyingType == typeof(DateTime) && result.Model is System.Collections.IEnumerable parsed)
                    {
                        var dates = parsed.Cast<DateTime>().Select(d => d.ToUniversalTime()).ToList();
                        if (dates.Count >= 2)
                            filter.AddProperty(BuildDateRangeExpression(property.Key.Name, dates.Min(), dates.Max()), null);
                    }
                    else
                    {
                        foreach (var v in (System.Collections.IEnumerable)result.Model)
                            filter.AddProperty(propName, propertyType, v);

                        // Mark as JSON array so FilterConstraintHandler generates LIKE instead of IN
                        if (JsonArrayProperties.Contains(propName) && filter.Properties?.TryGetValue(propName, out var fp) == true)
                            fp.IsJsonArray = true;
                    }
                }
            } 
            else if (propName.EndsWith(".isNull", StringComparison.OrdinalIgnoreCase))
            {
                // Handle ?field.isNull=true/false → field == null / field != null
                var baseName = propName[..^".isNull".Length];
                var isNullProp = EntityProperties.FirstOrDefault(p => p.Key.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));
                if (!isNullProp.Equals(default(KeyValuePair<ModelMetadata, IModelBinder>)))
                {
                    var actualPropName = isNullProp.Key.Name;
                    var isNull = parameters[propName][0] != "false";
                    var rawFilter = bindingContext.Model as IFilter;
                    rawFilter?.AddProperty(isNull ? $"{actualPropName} == null" : $"{actualPropName} != null", null);
                }
            }
            else
            {
                // think how secure it is
                var rawFilter = bindingContext.Model as IFilter;
                rawFilter?.AddProperty(propName, null);
            }
        }
    }

    /// <summary>
    /// Builds a DynamicLINQ half-open range expression
    /// </summary>
    private static string BuildDateRangeExpression(string propertyName, DateTime start, DateTime end)
    {
        return $"({propertyName} >= DateTime({start.Year}, {start.Month}, {start.Day}, {start.Hour}, {start.Minute}, {start.Second}) && " +
               $"{propertyName} < DateTime({end.Year}, {end.Month}, {end.Day}, {end.Hour}, {end.Minute}, {end.Second}))";
    }
}
