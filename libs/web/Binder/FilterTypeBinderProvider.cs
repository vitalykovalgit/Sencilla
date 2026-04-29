namespace Sencilla.Web;

/// <summary>
/// 
/// </summary>
[DisableInjection]
public class FilterTypeBinderProvider : IModelBinderProvider
{
    private IList<IInputFormatter> _formatters;

    public FilterTypeBinderProvider(IList<IInputFormatter> formatters)
    {
        _formatters = formatters;
    }

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (typeof(IFilter).IsAssignableFrom(context.Metadata.ModelType))
        {
            //var attr = context.Metadata.ModelType.GetCustomAttribute<FromBodyAttribute>();

            // Get properties from entity type                 
            var type = GetFilterEntityType(context.Metadata.ModelType);
            var entityProperties = type == null ? new ModelMetadata[] { } : context.MetadataProvider.GetMetadataForProperties(type);

            // make array from them
            var arrayEntityProperties = new Dictionary<string, ModelMetadata>();
            foreach (var property in entityProperties)
            {
                if (!property.ModelType.IsArray)
                {
                    var metadata = context.MetadataProvider.GetMetadataForType(property.ModelType.MakeArrayType());
                    arrayEntityProperties[property.ModelType.FullName] = metadata;
                }
            }

            // pass it to the binder 
            if (context.Metadata.IsComplexType)
            {
                var filterPropertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                foreach (var property in context.Metadata.Properties)
                    filterPropertyBinders.Add(property, context.CreateBinder(property));

                var entityPropertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                var jsonArrayProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var property in entityProperties ?? Enumerable.Empty<ModelMetadata>())
                {
                    var binderMetadata = context.MetadataProvider.GetMetadataForType(property.ModelType.MakeArrayType());
                    entityPropertyBinders.Add(property, context.CreateBinder(binderMetadata));

                    // Detect [JsonObjectString] — column stores a JSON array string,
                    // needs LIKE-based containment filter instead of IN.
                    var propInfo = type?.GetProperty(property.PropertyName ?? "", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (propInfo?.GetCustomAttribute<JsonObjectStringAttribute>() != null)
                        jsonArrayProperties.Add(property.PropertyName ?? "");
                }

                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                var readerFactory = context.Services.GetRequiredService<IHttpRequestStreamReaderFactory>();

                if (context.BindingInfo.BindingSource == BindingSource.Body)
                {
                    return new FilterTypeBodyBinder(loggerFactory,
                        filterPropertyBinders,
                        entityPropertyBinders,
                        _formatters,
                        readerFactory,
                        context.MetadataProvider.GetMetadataForType(typeof(object)));
                }
                else
                {
                    return new FilterTypeBinder(loggerFactory, filterPropertyBinders, entityPropertyBinders, arrayEntityProperties, jsonArrayProperties);
                }
            }
        }

        return null;
    }

    protected Type GetFilterEntityType(Type filterType)
    {
        Type baseType = filterType;
        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var generic = baseType.GetGenericTypeDefinition();
                if (generic == typeof(Filter<>))
                    return baseType.GetGenericArguments()[0];
            }

            baseType = baseType.BaseType;
        }

        return null;
    }
}
